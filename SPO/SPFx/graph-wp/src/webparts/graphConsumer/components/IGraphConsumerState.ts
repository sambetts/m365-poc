import { IUserItem } from "./IUserItem";

export interface IGraphConsumerState {
  users: Array<IUserItem> | null;
  searchFor: string;
}
