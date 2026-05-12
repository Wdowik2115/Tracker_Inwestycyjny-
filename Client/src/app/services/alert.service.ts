import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AlertDto, CreateAlertDto } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AlertService {
  private apiUrl = `${environment.apiUrl}/alerts`;

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