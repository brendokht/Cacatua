import { UserData } from "./UserData";

export interface ReturnUserData {
  user: UserData;
  jwt: string;
  message: string;
}
