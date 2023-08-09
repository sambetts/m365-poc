import { IAppContentLoader } from "../engine/IAppContentLoader";

export class FakeAppContentLoader implements IAppContentLoader {
    _nextUrl: string
    constructor() {
      this._nextUrl = "";
    }
  
    setNextUrl(nextUrl: string) {
      this._nextUrl = nextUrl;
    }
    loadCurrentItemUrl(scope: string): Promise<PlayListItem | null> {
      return Promise.resolve({ start: new Date(), end: new Date(), scope: scope, url: this._nextUrl });
    }
  }
  