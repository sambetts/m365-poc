
export function GetDatesBetween(earliest: Date, latest: Date, hourIncrease: number): Date[] {
    if (earliest < latest) {

        for (var arr = [], dt = new Date(earliest); dt <= new Date(latest); dt.setHours(dt.getHours() + hourIncrease)) {

            // Don't include if end of dt doesn't include time for 1 hour
            const testDt: Date = new Date(dt);
            testDt.setHours(dt.getHours() + hourIncrease);
            if (testDt <= latest) {
                arr.push(new Date(dt));
            }
        }
        return arr;
    }

    return [];
}

export function GetDatesExcluding(source: Date[], exclude: Date[]): Date[] {

    let results: Date[] = [];
    source.forEach(s => {

        let excluded = false;
        exclude.forEach(e => {
            if (s.getDate() === e.getDate()) {
                if (s.getHours() === e.getHours()) {
                    excluded = true;
                }
            }
        });

        if (!excluded) {
            results.push(s);
        }
    });
    return results;
}

export function addHour(dt: Date, hrs: number) {
    var date = new Date(dt.valueOf());
    date.setHours(date.getHours() + hrs);
    return date;
}
