import { ContosoClinicGraphLoader } from "./ContosoClinicGraphLoader";
import { User } from "@microsoft/microsoft-graph-types";
import { SingleItemLoaderCache } from "./abstract/SingleItemLoaderCache";


export class UserLoaderCache extends SingleItemLoaderCache<User> {

    _loader: ContosoClinicGraphLoader;
    constructor(loader: ContosoClinicGraphLoader) {
        super(async (id: string) => await this._loader.loadUserById(id))
        this._loader = loader;
    }
}
