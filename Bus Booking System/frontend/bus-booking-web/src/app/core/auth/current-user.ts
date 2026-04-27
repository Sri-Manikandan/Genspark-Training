export interface CurrentUser {
  id: string;
  name: string;
  email: string;
  phone: string | null;
  roles: string[];
}
