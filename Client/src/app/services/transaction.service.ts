import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TransactionDto, TransactionCreateDto } from '../models';

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  private apiUrl = 'http://localhost:5072/api/transactions';

  constructor(private http: HttpClient) {}

  getTransactions(): Observable<TransactionDto[]> {
    return this.http.get<TransactionDto[]>(this.apiUrl);
  }

  addTransaction(dto: TransactionCreateDto): Observable<TransactionDto> {
    return this.http.post<TransactionDto>(this.apiUrl, dto);
  }

  deleteTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}