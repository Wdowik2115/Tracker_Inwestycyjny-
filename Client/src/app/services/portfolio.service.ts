import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PortfolioSummaryDto } from '../models';

@Injectable({
  providedIn: 'root'
})
export class PortfolioService {
  private apiUrl = 'http://localhost:5072/api/portfolio';

  constructor(private http: HttpClient) { }

  getSummary(): Observable<PortfolioSummaryDto> {
    return this.http.get<PortfolioSummaryDto>(`${this.apiUrl}/summary`);
  }
}
