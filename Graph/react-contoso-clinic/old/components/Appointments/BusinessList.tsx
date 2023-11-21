

import { Button, DataGrid, DataGridBody, DataGridCell, DataGridHeader, DataGridHeaderCell, DataGridRow, TableCellLayout, TableColumnDefinition, createTableColumn } from "@fluentui/react-components";
import { BookingBusiness } from "@microsoft/microsoft-graph-types";

export function BusinessList(props: { businesses: BookingBusiness[], select: Function }) {

  const columns: TableColumnDefinition<BookingBusiness>[] = [

    createTableColumn<BookingBusiness>({
      columnId: "displayName",
      renderHeaderCell: () => {
        return "Name";
      },

      renderCell: (item) => {
        return item.displayName;
      },
    }),
    createTableColumn<BookingBusiness>({
      columnId: "select",
      renderHeaderCell: () => {
        return "";
      },
      renderCell: (item) => {
        return (
          <TableCellLayout>
            <Button onClick={props.select(item)}>Select</Button>
          </TableCellLayout>
        );
      },
    }),
  ];

  return (
    <>
      <table>
        <thead>
          <tr><th>Name</th></tr>
        </thead>
        <tbody>
          <tr>
            <td>asdfasdf</td>
          </tr>
        </tbody>
      </table>
      <DataGrid
        items={props.businesses}
        columns={columns}
        sortable
        selectionMode="multiselect"
        getRowId={(item: BookingBusiness) => item.id!}
        onSelectionChange={(e, data) => console.log(data)}
        focusMode="composite"
      >
        <DataGridHeader>
          <DataGridRow selectionCell={{ "aria-label": "Select all rows" }}>
            {({ renderHeaderCell }) => (
              <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
            )}
          </DataGridRow>
        </DataGridHeader>
        <DataGridBody<BookingBusiness>>
          {({ item, rowId }) => (
            <DataGridRow<BookingBusiness>
              key={rowId}
              selectionCell={{ "aria-label": "Select row" }}
            >
              {({ renderCell }) => (
                <DataGridCell>{renderCell(item)}</DataGridCell>
              )}
            </DataGridRow>
          )}
        </DataGridBody>
      </DataGrid>
    </>
  );
}
