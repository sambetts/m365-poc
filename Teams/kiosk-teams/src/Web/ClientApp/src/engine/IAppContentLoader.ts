import { getCurrentPlayListItem } from "../api/ApiCalls";

export class ApiAppContentLoader implements IAppContentLoader
{
    loadCurrentItemUrl(scope: string): Promise<PlayListItem | null> {
        return getCurrentPlayListItem(scope);
    }
}

export interface IAppContentLoader {
    loadCurrentItemUrl(scope: string): Promise<PlayListItem | null>
}
