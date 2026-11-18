export const environment = {
  apiUrl: ''
};

export function mediaUrl(path?: string | null): string {
  if (!path) return '';
  if (path.startsWith('http')) return path;
  return environment.apiUrl + path;
}
