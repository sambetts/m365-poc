import { ExampleAppGraphLoader } from "./ExampleAppGraphLoader";
import { User } from "@microsoft/microsoft-graph-types";
import { SingleItemLoaderCache } from "./abstract/SingleItemLoaderCache";


export class UserLoaderCache extends SingleItemLoaderCache<User> {

    _loader: ExampleAppGraphLoader;
    constructor(loader: ExampleAppGraphLoader) {
        super(async (id: string) => await this._loader.loadUserById(id))
        this._loader = loader;
    }
}
