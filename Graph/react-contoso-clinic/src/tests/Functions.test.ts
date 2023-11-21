import { GetDatesBetween, GetDatesExcluding, addHour } from '../services/DateFunctions';

test('Date functions', () => {
  const now = new Date();
  expect(addHour(now, 1) > now).toBeTruthy();
});


test('GetDatesBetween', () => {

  expect(GetDatesBetween(new Date("2018-08-01"), new Date("2018-07-01"), 1).length === 0).toBeTruthy();

  const now = new Date();
  expect(GetDatesBetween(now, addHour(now, 8), 1).length === 8).toBeTruthy();

});

test('GetDatesExcluding', () => {

  const now = new Date();
  const list1 = [now, addHour(now, 1)];

  const list2 = [now];
  expect(GetDatesExcluding(list1, list2).length === 1).toBeTruthy();
});
