export const wait = function ({ms = 1000}) {
    return new Promise(resolve => {
        setTimeout(resolve, ms);
    });
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export async function longPollSync(api_path: string, lp_message: string, callback: (data: object, setState: React.Dispatch<React.SetStateAction<any[]>>) => void, setState: React.Dispatch<React.SetStateAction<any[]>>) {
  const timeout = 20_000;
  const controller = new AbortController();

  const timeoutId = setTimeout(() => {
    controller.abort();
  }, timeout);

  await fetch(`${api_path}`, {
    method: "GET",
    credentials: "include",
    signal: controller.signal
  }).then(resolved => {
    clearTimeout(timeoutId);
    if (!resolved.ok) {
      throw new Error("Long poll network response not ok");
    }
    return resolved.json();
  }).then(data => {
    callback(data, setState);
    return;
  }).catch(error => {
    if (error.name === "AbortError") {
      // AbortError means that the timeout occured
      //console.log("abort or timeout")
    } else {
      console.error("Error during " + lp_message + " long-poll: ", error);
    }
  });
}

export async function longPollSyncWhile(path: string, api_path: string, lp_message: string, callback: (data:object) => void) {
  while (window.location.pathname.startsWith(path)) {
    console.log("started " + path + " sync");

    const timeout = 20_000;

    const controller = new AbortController();
    const signal = controller.signal;

    const timeoutId = setTimeout(() => {
      controller.abort();
    }, timeout);

    await fetch(`${api_path}`, {
      method: "GET",
      credentials: "include",
      signal: signal
    }).then(resolved => {
      clearTimeout(timeoutId);
      if (!resolved.ok) {
        throw new Error("Long poll network response not ok");
      }
      return resolved.json();
    }).then(data => {
      callback(data);
      return;
    }).catch(error => {
      if (error.name === "AbortError") {
        // AbortError means that the timeout occured
        //console.log("timeout")
      } else {
        console.error("Error during " + lp_message + " long-poll: ", error);
      }
    });
  }
}