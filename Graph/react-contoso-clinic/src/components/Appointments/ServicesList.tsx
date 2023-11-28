

import { BookingBusiness, BookingService } from "@microsoft/microsoft-graph-types";

export function ServicesList(props: { forBusiness: BookingBusiness, data: BookingService[]}) {

  return (
    <>
      <table className="table">
        <thead>
          <tr>
            <th>Name</th>
          </tr>
        </thead>
        <tbody>
          {props.data.map((b: BookingService) => {
            return <tr key={b.id}>
              <td>
                {b.displayName}
              </td>
            </tr>
          })
          }
        </tbody>
      </table>
    </>
  );
}
