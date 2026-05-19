import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject, tap } from 'rxjs';
import { TransactionDto, TransactionCreateDto, TransactionUpdateDto } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  private apiUrl = `${environment.apiUrl}/transactions`;

  private transactionAddedSubject = new Subject<void>();
  transactionAdded$ = this.transactionAddedSubject.asObservable();

  constructor(private http: HttpClient) {}

  getTransactions(params?: { 
    page?: number; 
    pageSize?: number; 
    walletId?: string; 
    symbol?: string; 
    startDate?: string; 
    endDate?: string; 
  }): Observable<{ items: TransactionDto[]; totalCount: number }> {
    let httpParams = new HttpParams();
    
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          httpParams = httpParams.set(key, value.toString());
        }
      });
    }

    return this.http.get<{ items: TransactionDto[]; totalCount: number }>(this.apiUrl, { params: httpParams });
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