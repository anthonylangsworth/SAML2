﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;
using Owin.Security.Saml;
using SAML2;
using SAML2.Config;

namespace TestSSO
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(SamlAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions {
                AuthenticationType = "SAML2"
            });

            Saml2Configuration saml2Configuration = new Saml2Configuration
            {
                ServiceProvider = new ServiceProvider {
                    SigningCertificate = new X509Certificate2(FileEmbeddedResource("TestSSO.sp.pfx"), "password"),
                    Server = "https://localhost:44338",
                    Id = "https://localhost:44338"
                },
                AllowedAudienceUris = new List<Uri>(new[] { new Uri("https://localhost:44338") })
            };
            saml2Configuration.ServiceProvider.Endpoints.AddRange(new[] {
                new ServiceProviderEndpoint
                {
                    Type = EndpointType.SignOn,
                    LocalPath  = "/account/signin",
                    RedirectUrl = "/account/signin"
                },
                new ServiceProviderEndpoint
                {
                    Type = EndpointType.Logout,
                    LocalPath  = "/account/signout",
                    RedirectUrl = "/account/signout"
                }
            });
            // testshib-providers.xml is not supported because it contains an <EntitiesDescription> element
            if (!saml2Configuration.IdentityProviders.TryAddByMetadata(@"c:\users\anthony\source\SAML2\src\TestSSO\idpMetadata.xml"))
            {
                throw new ArgumentException("Invalid metadata file");
            }
            saml2Configuration.IdentityProviders.First().Default = true;
            saml2Configuration.LoggingFactoryType = "SAML2.Logging.DebugLoggerFactory";

            app.UseSamlAuthentication(
                new SamlAuthenticationOptions
                {
                    Configuration = saml2Configuration,
                    RedirectAfterLogin = "/",
                    AuthenticationMode = AuthenticationMode.Active // Should capture 401 events but does not
                });
        }

        private byte[] FileEmbeddedResource(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = path;

            byte[] result = null;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                result = memoryStream.ToArray();
            }
            return result;
        }
    }
}