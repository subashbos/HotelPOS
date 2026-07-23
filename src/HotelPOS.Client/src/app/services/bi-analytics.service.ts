import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { environment } from '../../environments/environment';

export interface BiOverviewKpis {
  totalRevenue: number;
  netProfit: number;
  foodCostPercentage: number;
  totalWastageCost: number;
  cogs: number;
  totalExpenses: number;
}

export interface MonthlyTrendBar {
  monthName: string;
  revenue: number;
  profit: number;
}

@Injectable({
  providedIn: 'root'
})
export class BiAnalyticsService {
  private readonly apiUrl = `${environment.apiBaseUrl}/reports/bi-analytics`;

  constructor(private readonly http: HttpClient) { }

  getBiAnalytics(fromDate?: string, toDate?: string): Observable<{
    kpis: BiOverviewKpis;
    monthlyTrends: MonthlyTrendBar[];
  }> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<{
      kpis: BiOverviewKpis;
      monthlyTrends: MonthlyTrendBar[];
    }>(this.apiUrl, { params });
  }
}
