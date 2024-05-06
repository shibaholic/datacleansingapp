using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace dc_app.ServiceLibrary.Utilities;

public static class CryptographyUtility
{
    public static string Generate11CharRandomBase64UrlString()
    {
        // Convert.ToBase64String uses 6 bits to make a base64 char instead of the full 8 bits of a byte.
        string random_base64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8));
        // Convert.ToBase64String's 62nd and 63rd characters are '+' and '/', which are not uri friendly.
        // Also remove packing byte '=' and make sure it is 11 chars length.
        string base64url = random_base64.Substring(0,11).Replace('/', '_').Replace('+', '-');
        return base64url;
    }
}
