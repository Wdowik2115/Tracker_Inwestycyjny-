import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject, tap } from 'rxjs';
import { TransactionDto, TransactionCreateDto, TransactionUpdateDto } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  private apiUrl = `${environment.apiUrl}/transactions`;

  // Signal to notify other components that data has changed
  private transactionAddedSubject = new Subject<void>();
  transactionAdded$ = this.transactionAddedSubject.asObservable();

  constructor(private http: HttpClient) {}

  getTransactions(): Observable<TransactionDto[]> {
    return this.http.get<TransactionDto[]>(this.apiUrl);
  }

  addTransaction(dto: TransactionCreateDto): Observable<TransactionDto> {
    return this.http.post<TransactionDto>(this.apiUrl, dto).pipe(
      tap(() => this.transactionAddedSubject.next())
    );
  }

  updateTransaction(id: string, dto: TransactionUpdateDto): Observable<TransactionDto> {
    return this.http.put<TransactionDto>(`${this.apiUrl}/${id}`, dto).pipe(
      tap(() => this.transactionAddedSubject.next())
    );
  }

  deleteTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.transactionAddedSubject.next())
    );
  }
}