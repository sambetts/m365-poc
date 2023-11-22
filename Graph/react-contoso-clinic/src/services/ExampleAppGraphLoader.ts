import { GraphLoader } from "./GraphLoader";
import { BookingAppointment, BookingBusiness, BookingStaffMember, User } from "@microsoft/microsoft-graph-types";

const MAX_ITEMS : number = 5;

// App-specific implementation for GraphLoader
export class ExampleAppGraphLoader extends GraphLoader {

    loadUserProfile(): Promise<User> {
        return this.loadSingle<User>("/me");
    }

    loadUserById(id: string): Promise<User> {
        return this.loadSingle<User>("/users/" + id);
    }

    loadStaffMemberById(businessId: string, staffMemberId: string): Promise<BookingStaffMember> {
        return this.loadSingle<BookingStaffMember>(`/solutions/bookingBusinesses/${businessId}/staffMembers/${staffMemberId}`);
    }

    loadBookingBusinesses(): Promise<BookingBusiness[]> {
        return this.loadList<BookingBusiness[]>("/solutions/bookingBusinesses", MAX_ITEMS);
    }

    loadBusinessAppointments(businessId: string): Promise<BookingAppointment[]> {
        return this.loadList<BookingAppointment[]>(`/solutions/bookingBusinesses/${businessId}/appointments`, MAX_ITEMS);
    }
}

