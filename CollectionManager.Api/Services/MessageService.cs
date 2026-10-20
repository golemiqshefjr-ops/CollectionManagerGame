using System.Security.Claims;
using CollectionManager.Api.Data;
using Microsoft.Data.SqlClient;

namespace CollectionManager.Api.Services;

public sealed class MessageService(SqlConnectionFactory factory, NotificationService notifications)
{
    public async Task<IReadOnlyList<object>> Contacts(ClaimsPrincipal user)
    {
        await using var db=factory.Create(); await db.OpenAsync(); await using var cmd=new SqlCommand("SELECT u.Id,u.DisplayName,u.AvatarUrl,MAX(m.SentAt) LastMessage FROM dbo.Messages m JOIN dbo.Users u ON u.Id=CASE WHEN m.SenderId=@me THEN m.RecipientId ELSE m.SenderId END WHERE m.SenderId=@me OR m.RecipientId=@me GROUP BY u.Id,u.DisplayName,u.AvatarUrl ORDER BY MAX(m.SentAt) DESC",db);
        cmd.Parameters.AddWithValue("@me",user.UserId()); var result=new List<object>(); await using var r=await cmd.ExecuteReaderAsync(); while(await r.ReadAsync()) result.Add(new {id=r.GetInt32(0),name=r.GetString(1),avatarUrl=r.IsDBNull(2)?null:r.GetString(2),lastMessage=r.GetDateTime(3)}); return result;
    }
    public async Task<IReadOnlyList<MessageDto>> Conversation(int otherUserId, ClaimsPrincipal user)
    {
        var id=user.UserId(); await using var db=factory.Create(); await db.OpenAsync();
        await using var cmd=new SqlCommand("SELECT Id,SenderId,RecipientId,ItemId,Body,SentAt FROM dbo.Messages WHERE (SenderId=@me AND RecipientId=@other) OR (SenderId=@other AND RecipientId=@me) ORDER BY SentAt",db);
        cmd.Parameters.AddWithValue("@me",id); cmd.Parameters.AddWithValue("@other",otherUserId); var result=new List<MessageDto>(); await using var r=await cmd.ExecuteReaderAsync();
        while(await r.ReadAsync()) result.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetInt32(2),r.IsDBNull(3)?null:r.GetInt32(3),r.GetString(4),r.GetDateTime(5))); return result;
    }
    public async Task<MessageDto> Send(MessageInput input, ClaimsPrincipal user)
    {
        var me=user.UserId(); if(input.RecipientId==me || input.Text.Trim().Length is < 1 or > 2000) throw ServiceHelpers.BadRequest("Съобщението или получателят са невалидни.");
        await using var db=factory.Create(); await db.OpenAsync();
        await using var cmd=new SqlCommand("INSERT dbo.Messages(SenderId,RecipientId,ItemId,Body) OUTPUT INSERTED.Id,INSERTED.SenderId,INSERTED.RecipientId,INSERTED.ItemId,INSERTED.Body,INSERTED.SentAt VALUES(@me,@to,@item,@body)",db);
        cmd.Parameters.AddWithValue("@me",me); cmd.Parameters.AddWithValue("@to",input.RecipientId); cmd.Parameters.AddWithValue("@item",(object?)input.ItemId??DBNull.Value); cmd.Parameters.AddWithValue("@body",input.Text.Trim());
        await using var r=await cmd.ExecuteReaderAsync(); if(!await r.ReadAsync()) throw ServiceHelpers.Conflict("Съобщението не беше изпратено.");
        var message = new MessageDto(r.GetInt32(0),r.GetInt32(1),r.GetInt32(2),r.IsDBNull(3)?null:r.GetInt32(3),r.GetString(4),r.GetDateTime(5));
        await r.CloseAsync();
        await notifications.CreateAsync(message.RecipientId, message.SenderId, message.ItemId, "message", "Ново съобщение", "Имаш ново съобщение от колекционер.");
        return message;
    }
}

