import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TransactionDto, TransactionCreateDto, TransactionUpdateDto } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  private apiUrl = `${environment.apiUrl}/transactions`;

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
}