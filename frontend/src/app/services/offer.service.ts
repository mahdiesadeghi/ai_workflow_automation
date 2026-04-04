import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { OfferInfo } from '../models/workflow.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OfferService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getOffers(): Observable<OfferInfo[]> {
    return this.http.get<OfferInfo[]>(`${this.baseUrl}/offers`);
  }

  searchOffers(planType: string, maxPrice: number): Observable<OfferInfo[]> {
    const params = new HttpParams()
      .set('planType', planType)
      .set('maxPrice', maxPrice.toString());
    return this.http.get<OfferInfo[]>(`${this.baseUrl}/offers/search`, { params });
  }
}
