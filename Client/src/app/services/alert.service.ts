import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AlertDto, CreateAlertDto, UpdateAlertDto } from '../models';
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

  getAlertById(id: string): Observable<AlertDto> {
    return this.http.get<AlertDto>(`${this.apiUrl}/${id}`);
  }

  createAlert(dto: CreateAlertDto): Observable<AlertDto> {
    return this.http.post<AlertDto>(this.apiUrl, dto);
  }

  updateAlert(id: string, dto: UpdateAlertDto): Observable<AlertDto> {
    return this.http.put<AlertDto>(`${this.apiUrl}/${id}`, dto);
  }

  deleteAlert(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  resetAlert(id: string): Observable<AlertDto> {
    return this.http.post<AlertDto>(`${this.apiUrl}/${id}/reset`, {});
  }
}
