import { ExampleAppGraphLoader } from "./ExampleAppGraphLoader";
import { User } from "@microsoft/microsoft-graph-types";


export class UserLoaderCache {
    _loader: ExampleAppGraphLoader;
    _cache: UserCache[] = [];
    constructor(loader: ExampleAppGraphLoader) {
        this._loader = loader;
    }

    async loadUserProfile(id: string): Promise<User | null> {

        let cache: User | null = null;

        this._cache.forEach(c => {
            if (c.id === id) {
                cache = c;
                return;
            }
        });
        if (!cache) {
            try {
                cache = await this._loader.loadUserById(id);
            } catch (error) {
                cache = null;
            }
            this._cache.push({ id: id, user: cache });
        }
        return cache;
    }
}

interface UserCache {
    id: string,
    user: User | null
}