import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WalletDto, CreateWalletDto } from '../models';

@Injectable({
  providedIn: 'root'
})
export class WalletService {
  private apiUrl = 'http://localhost:5072/api/wallets';

  constructor(private http: HttpClient) { }

  getWallets(): Observable<WalletDto[]> {
    return this.http.get<WalletDto[]>(this.apiUrl);
  }

  createWallet(dto: CreateWalletDto): Observable<WalletDto> {
    return this.http.post<WalletDto>(this.apiUrl, dto);
  }

  deleteWallet(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
