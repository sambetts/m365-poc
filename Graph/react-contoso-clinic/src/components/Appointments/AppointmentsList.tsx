

import { BookingAppointment, BookingCustomerInformation } from "@microsoft/microsoft-graph-types";
import { UserLoaderCache } from "../../services/UserLoaderCache";
import AzureAdUser from "../AzureAdUser";

export function AppointmentsList(props: { data: BookingAppointment[], userLoader: UserLoaderCache }) {

  return (
    <>
      <table>
        <tbody>
          {props.data.map((b: BookingAppointment) => {
            return <tr key={b.id}>
              <td>
                {b.customers &&
                  <>
                    {b.customers.map((c: BookingCustomerInformation) => {
                      return <div>{c.name}</div>
                    })
                    }
                  </>
                }
              </td>
              <td>@
                {b.startDateTime?.dateTime &&
                  <>{new Date(b.startDateTime?.dateTime).toLocaleDateString("en-GB")}</>
                }
              </td>
              <td>
                {b.staffMemberIds &&
                  <>
                    {b.staffMemberIds.map(id => {
                      return <AzureAdUser loader={props.userLoader} userId={id} />
                    })}
                  </>
                }

              </td>
            </tr>
          })
          }
        </tbody>
      </table>
    </>
  );
}
