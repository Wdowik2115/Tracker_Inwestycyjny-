import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AlertDto, CreateAlertDto } from '../models';

@Injectable({
  providedIn: 'root'
})
export class AlertService {
  private apiUrl = 'http://localhost:5072/api/alerts';

  constructor(private http: HttpClient) {}

  getAlerts(): Observable<AlertDto[]> {
    return this.http.get<AlertDto[]>(this.apiUrl);
  }

  createAlert(dto: CreateAlertDto): Observable<AlertDto> {
    return this.http.post<AlertDto>(this.apiUrl, dto);
  }

  deleteAlert(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}