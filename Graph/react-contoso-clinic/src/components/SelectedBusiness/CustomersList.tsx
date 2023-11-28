

import { BookingBusiness, BookingCustomer } from "@microsoft/microsoft-graph-types";

export function CustomersList(props: { forBusiness: BookingBusiness, data: BookingCustomer[]}) {

  return (
    <>
      <table className="table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
          </tr>
        </thead>
        <tbody>
          {props.data.map((b: BookingCustomer) => {
            return <tr key={b.id}>
              <td>
                {b.displayName}
              </td>
              <td>
                {b.emailAddress}
              </td>
            </tr>
          })
          }
        </tbody>
      </table>
    </>
  );
}
