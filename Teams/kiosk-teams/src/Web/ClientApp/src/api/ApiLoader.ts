

// Load and handle error
export const loadFromApi = async (url: string, method: string, body?: any, onError?: Function): Promise<string> => {

    var req: any = {
        method: method,
        headers: {
            'Content-Type': 'application/json',
        },
        body: null
    };
    if (body)
        req.body = JSON.stringify(body);

    console.info(`Loading ${url}...`);

    return fetch(url, req)
        .then(async response => {

            if (response.ok) {
                const dataText: string = await response.text();
                console.info(`${url}: '${dataText}'`);
                return Promise.resolve(dataText);
            }
            else {
                const dataText: string = await response.text();
                if (!onError) {
                    const errorTitle = `Error ${response.status} ${method}ing to/from API '${url}'`;

                    if (dataText !== "")
                        alert(`${errorTitle}: ${dataText}`)
                    else
                        alert(errorTitle);
                }
                else
                    onError(dataText);
                return Promise.reject(dataText);
            }
        });
};
