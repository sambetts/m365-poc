import { GraphLoader } from "./abstract/GraphLoader";
import { BookingAppointment, BookingBusiness, BookingCustomer, BookingStaffMember, User } from "@microsoft/microsoft-graph-types";

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

    loadBusinessStaffMembers(businessId: string): Promise<BookingStaffMember[]> {
        return this.loadList<BookingStaffMember[]>(`/solutions/bookingBusinesses/${businessId}/staffMembers`, MAX_ITEMS);
    }

    loadBusinessCustomers(businessId: string): Promise<BookingCustomer[]> {
        return this.loadList<BookingStaffMember[]>(`/solutions/bookingBusinesses/${businessId}/customers`, MAX_ITEMS);
    }
    loadBusinessCustomerByGraphUser(businessId: string, user: User): Promise<BookingCustomer | null | undefined> {
        if (!user.displayName) {
            throw new Error("Invalid user");
        }
        return this.loadBusinessCustomers(businessId).then((r: BookingStaffMember[]) => 
        {
            return Promise.resolve(r.find(c=> c.emailAddress === user.mail));
        });
    }

    createBusinessCustomer(businessId: string, user: User): Promise<BookingCustomer> {
        if (!user.displayName) {
            throw new Error("Invalid user");
        }
        const newUser: BookingCustomer = 
        {
            emailAddress: user.mail,
            displayName: user.displayName
        }
        return this.loadSinglePost<BookingCustomer>(`/solutions/bookingBusinesses/${businessId}/customers`, newUser);
    }
}

