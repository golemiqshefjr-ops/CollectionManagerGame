(() => {
  const TOKEN_KEY = 'collection.jwt';
  window.api = {
    get token() { return localStorage.getItem(TOKEN_KEY); },
    set token(value) { value ? localStorage.setItem(TOKEN_KEY, value) : localStorage.removeItem(TOKEN_KEY); },
    async request(path, options = {}) {
      const headers = new Headers(options.headers || {});
      headers.set('Accept', 'application/json');
      if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json');
      if (this.token) headers.set('Authorization', `Bearer ${this.token}`);
      const response = await fetch(path, { ...options, headers });
      if (response.status === 401) {
        this.token = null;
        window.dispatchEvent(new CustomEvent('auth:expired'));
        throw new Error('Сесията изтече. Влез отново.');
      }
      if (response.status === 403) throw new Error('Нямаш право за това действие.');
      if (response.status === 204) return null;
      const content = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(content.error || `Заявката не успя (${response.status}).`);
      return content;
    },
    get(path) { return this.request(path); },
    post(path, body) { return this.request(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) }); },
    put(path, body) { return this.request(path, { method: 'PUT', body: JSON.stringify(body) }); },
    delete(path) { return this.request(path, { method: 'DELETE' }); }
  };
})();

