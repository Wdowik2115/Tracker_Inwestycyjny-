import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { WatchlistItemDto, AddToWatchlistDto } from '../models';

@Injectable({
  providedIn: 'root'
})
export class WatchlistService {
  private readonly apiUrl = `${environment.apiUrl}/watchlist`;

  constructor(private http: HttpClient) {}

  getWatchlist(): Observable<WatchlistItemDto[]> {
    return this.http.get<WatchlistItemDto[]>(this.apiUrl);
  }

  addToWatchlist(dto: AddToWatchlistDto): Observable<WatchlistItemDto> {
    return this.http.post<WatchlistItemDto>(this.apiUrl, dto);
  }

  removeFromWatchlist(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  isOnWatchlist(coinId: string): Observable<{ isOnWatchlist: boolean }> {
    return this.http.get<{ isOnWatchlist: boolean }>(`${this.apiUrl}/check/${coinId}`);
  }

  getSuggestions(query: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/suggestions`, { params: { query } });
  }
}
