import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WalletDto, CreateWalletDto, WalletDetailsDto } from '../models';
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

  deleteWallet(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
