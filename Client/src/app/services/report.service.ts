import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ReportDto } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private apiUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  getReports(): Observable<ReportDto[]> {
    return this.http.get<ReportDto[]>(this.apiUrl);
  }

  generateAccountReport(): Observable<ReportDto> {
    return this.http.post<ReportDto>(`${this.apiUrl}/account`, {});
  }

  generateWalletReport(walletId: string): Observable<ReportDto> {
    return this.http.post<ReportDto>(`${this.apiUrl}/wallet/${walletId}`, {});
  }

  downloadReport(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/download`, { responseType: 'blob' });
  }

  deleteReport(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
