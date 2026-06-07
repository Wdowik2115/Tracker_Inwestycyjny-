import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TransactionDto, TransactionCreateDto, TransactionUpdateDto } from '../models';
import { environment } from '../../environments/environment';

export interface CoinSearchDto {
  coinId: string;
  symbol: string;
  name: string;
  imageUrl?: string;
}

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  private apiUrl = `${environment.apiUrl}/transactions`;
  private coinsApiUrl = `${environment.apiUrl}/coins`;

  constructor(private http: HttpClient) {}

  getTransactions(): Observable<TransactionDto[]> {
    return this.http.get<TransactionDto[]>(this.apiUrl);
  }

  addTransaction(dto: TransactionCreateDto): Observable<TransactionDto> {
    return this.http.post<TransactionDto>(this.apiUrl, dto);
  }

  updateTransaction(id: string, dto: TransactionUpdateDto): Observable<TransactionDto> {
    return this.http.put<TransactionDto>(`${this.apiUrl}/${id}`, dto);
  }

  deleteTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  searchCoins(query: string): Observable<CoinSearchDto[]> {
    return this.http.get<CoinSearchDto[]>(`${this.coinsApiUrl}/search`, {
      params: { query }
    });
  }
}