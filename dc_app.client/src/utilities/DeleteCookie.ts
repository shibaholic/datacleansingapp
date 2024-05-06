function delete_cookie(name:string, path:string, domain:string) {
  if (get_cookie(name)) {
    document.cookie = name + "=" +
      ((path) ? ";path=" + path : "") +
      ((domain) ? ";domain=" + domain : "") +
      ";expires=Thu, 01 Jan 1970 00:00:01 GMT";
  }
}

function get_cookie(name:string) {
  return document.cookie.split(';').some(c => {
    return c.trim().startsWith(name + '=');
  });
}

export default delete_cookie;