

import { BookingBusiness } from "@microsoft/microsoft-graph-types";
import { Button } from "react-bootstrap";

export function BusinessList(props: { businesses: BookingBusiness[], select: Function }) {

  return (
    <>

      <table>
        <thead>
          <tr><th>Name</th><th></th></tr>
        </thead>
        <tbody>
          {props.businesses.map((b: BookingBusiness) => {
            return <tr>
              <td>{b.displayName}</td>
              <td><Button onClick={()=> props.select(b)}>Select</Button></td>
            </tr>
          })
          }

        </tbody>
      </table>
    </>
  );
}
