import { GraphLoader } from "./abstract/GraphLoader";
import { BookingAppointment, BookingBusiness, BookingCustomer, BookingService, BookingStaffMember, User } from "@microsoft/microsoft-graph-types";

const MAX_ITEMS : number = 5;

// App-specific implementation for GraphLoader
export class ContosoClinicGraphLoader extends GraphLoader {

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
    
    createAppointment(businessId: string, appointment: BookingAppointment): Promise<BookingAppointment> {
        return this.loadSinglePost<BookingAppointment>(`/solutions/bookingBusinesses/${businessId}/appointments`, appointment);
    }

    loadBusinessStaffMembers(businessId: string): Promise<BookingStaffMember[]> {
        return this.loadList<BookingStaffMember[]>(`/solutions/bookingBusinesses/${businessId}/staffMembers`, MAX_ITEMS);
    }

    loadBusinessServices(businessId: string): Promise<BookingService[]> {
        return this.loadList<BookingService[]>(`/solutions/bookingBusinesses/${businessId}/services`, MAX_ITEMS);
    }

    loadBusinessCustomers(businessId: string): Promise<BookingCustomer[]> {
        return this.loadList<BookingStaffMember[]>(`/solutions/bookingBusinesses/${businessId}/customers`, MAX_ITEMS);
    }
    loadBusinessCustomerByGraphUser(businessId: string, user: User, usersLoaded? : Function): Promise<BookingCustomer | null | undefined> {
        if (!user.displayName) {
            throw new Error("Invalid user");
        }
        return this.loadBusinessCustomers(businessId).then((r: BookingStaffMember[]) => 
        {
            if (usersLoaded) {
                usersLoaded(r);
            }
            return Promise.resolve(r.find(c=> c.emailAddress === user.userPrincipalName));
        });
    }

    createBusinessCustomer(businessId: string, user: User): Promise<BookingCustomer> {
        if (!user.displayName) {
            throw new Error("Invalid user");
        }

        const bookingCustomerBase = {
            '@odata.type': '#microsoft.graph.bookingCustomer',
            displayName: user.displayName,
            emailAddress: user.userPrincipalName,
            addresses: [],
            phones: []
        };
        
        return this.loadSinglePost<BookingCustomer>(`/solutions/bookingBusinesses/${businessId}/customers`, bookingCustomerBase);
    }
}
