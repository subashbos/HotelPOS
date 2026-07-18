export interface Supplier {
  id: number;
  name: string;
  contactPerson?: string;
  phone?: string;
  email?: string;
  address?: string;
  gstin?: string;
  city?: string;
  state?: string;
  pincode?: string;
  openingBalance: number;
  creditLimit: number;
  paymentTerms?: string;
}
