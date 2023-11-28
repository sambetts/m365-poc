import { ContosoClinicGraphLoader } from "./ContosoClinicGraphLoader";
import { BookingStaffMember, User } from "@microsoft/microsoft-graph-types";
import { SingleItemLoaderCache } from "./abstract/SingleItemLoaderCache";


export class UserLoaderCache extends SingleItemLoaderCache<User> {

    _loader: ContosoClinicGraphLoader;
    constructor(loader: ContosoClinicGraphLoader) {
        super(async (id: string) => await this._loader.loadUserById(id))
        this._loader = loader;
    }
}

export class StaffMemberLoaderCache extends SingleItemLoaderCache<BookingStaffMember> {

    _loader: ContosoClinicGraphLoader;
    constructor(loader: ContosoClinicGraphLoader, businessId: string) {
        super(async (id: string) => await this._loader.loadStaffMemberById(businessId, id))
        this._loader = loader;
    }
}
