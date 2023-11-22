
// Generic cache handler
export abstract class SingleItemLoaderCache<T> {
    _loaderCallbackWithIdParam: Function;
    _cache: SingleItemCache<T>[] = [];

    constructor(loaderCallbackWithIdParam: Function) {
        this._loaderCallbackWithIdParam = loaderCallbackWithIdParam;
    }

    async loadFromCacheOrAPI(id: string): Promise<T | null> {

        let cachedObject: T | null = null;

        this._cache.forEach(c => {
            if (c.id === id) {
                cachedObject = c.obj;
                return;
            }
        });

        if (!cachedObject) {
            try {
                cachedObject = await this._loaderCallbackWithIdParam(id);
            } catch (error) {
                cachedObject = null;
            }
            this._cache.push({ id: id, obj: cachedObject });
        }

        return cachedObject;
    }
}

interface SingleItemCache<T> {
    id: string,
    obj: T | null
}