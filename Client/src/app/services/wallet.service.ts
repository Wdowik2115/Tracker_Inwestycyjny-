import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WalletDto, CreateWalletDto, UpdateWalletDto, WalletDetailsDto, WalletHistoryDto } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class WalletService {
  private apiUrl = `${environment.apiUrl}/wallets`;

  constructor(private http: HttpClient) { }

  getWallets(): Observable<WalletDto[]> {
    return this.http.get<WalletDto[]>(this.apiUrl);
  }

  getWallet(id: string): Observable<WalletDetailsDto> {
    return this.http.get<WalletDetailsDto>(`${this.apiUrl}/${id}`);
  }

  createWallet(dto: CreateWalletDto): Observable<WalletDto> {
    return this.http.post<WalletDto>(this.apiUrl, dto);
  }

  updateWallet(id: string, dto: UpdateWalletDto): Observable<WalletDto> {
    return this.http.put<WalletDto>(`${this.apiUrl}/${id}`, dto);
  }

  deleteWallet(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getWalletHistory(id: string, days = 30): Observable<WalletHistoryDto> {
    return this.http.get<WalletHistoryDto>(`${this.apiUrl}/${id}/history?days=${days}`);
  }
}
