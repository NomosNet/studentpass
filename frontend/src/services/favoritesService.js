import { apiRequest } from './apiClient'

export function getFavorites(params = {}) {
  return apiRequest('/api/v1/favorites', { query: params })
}

export function addFavorite(adId) {
  return apiRequest(`/api/v1/favorites/${adId}`, { method: 'POST' })
}

export function removeFavorite(adId) {
  return apiRequest(`/api/v1/favorites/${adId}`, { method: 'DELETE' })
}
