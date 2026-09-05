let axios: any = null;
try {
  axios = require('axios');
} catch {
  axios = null;
}

/**
 * Enterprise Axios HTTP WebAPI Client
 */
export async function fetchApiData(url: string, headers?: Record<string, string>): Promise<any> {
  try {
    if (axios) {
      const response = await axios.get(url, { headers: headers || {} });
      return response.data;
    }
    const res = await fetch(url, { headers });
    return await res.json();
  } catch (e) {
    console.error('API fetch error:', e);
    return null;
  }
}

export async function postApiData(url: string, payload: any, headers?: Record<string, string>): Promise<any> {
  try {
    if (axios) {
      const response = await axios.post(url, payload, { headers: headers || {} });
      return response.data;
    }
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', ...(headers || {}) },
      body: JSON.stringify(payload),
    });
    return await res.json();
  } catch (e) {
    console.error('API post error:', e);
    return null;
  }
}
