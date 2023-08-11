using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net.Mime;
using System.Security.AccessControl;
using System.Threading.Tasks;
using ASP_T11.Entities;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Novell.Directory.Ldap;
using LdapConnection = Novell.Directory.Ldap.LdapConnection;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ASP_T11.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {

        [HttpPost("ldap")]
        public Task<IActionResult> LdapLogin(LoginRequest body)
        {
            // LOGIC
            string LDAP_PATH = "10.36.0.2";
            string LDAP_DN = "npcetc";
            string SEARCH_BASE = "dc=npcetc,dc=vn";
            string FILTER = $"(cn={body.Username})";
            //string USER = "downloader";
            //string PASSWORD = "Npcetc@2021";

            try
            {
                LdapConnection cn = new LdapConnection();
                cn.Connect(LDAP_PATH, LdapConnection.DefaultPort);

                Console.WriteLine(LDAP_DN + "\\" + body.Username);
                cn.Bind(LdapConnection.LdapV3, LDAP_DN + "\\" + body.Username, body.Password);

                if (cn.Bound)
                {
                    Console.WriteLine("Login successs");
                    var searchResult = cn.Search(SEARCH_BASE, LdapConnection.ScopeSub, FILTER, null, false);
                    Console.WriteLine(searchResult);
                    Console.WriteLine(searchResult.HasMore());
                    if (searchResult.HasMore())
                    {
                        Console.WriteLine("NEXT .... ");
                        try
                        {
                            var entry = searchResult.Next();
                            Console.WriteLine(entry);
                            Console.WriteLine("-----------------------------");
                            string email = entry.GetAttribute("mail").StringValue;
                            string department = entry.GetAttribute("department").StringValue;
                            string name = entry.GetAttribute("cn").StringValue;

                            return Task.FromResult<IActionResult>(Ok(new
                            {
                                Message = "ok",
                                Name = name,
                                Email = email,
                                Department = department,
                            }));
                        }
                        catch (Exception ex)
                        {
                            return Task.FromResult<IActionResult>(BadRequest("EX " + ex.Message));
                        }
                    }
                }
                    
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Task.FromResult<IActionResult>(BadRequest(ex.Message));
            }
            return Task.FromResult<IActionResult>(Ok(body));
        }


        [HttpPost("upload")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            string endpoint = "minio.npcetc.vn:9000";
            string accessKey = "admin";
            string secretKey = "";


            MinioClient _minioClient = new MinioClient()
                              .WithEndpoint(endpoint)
                              .WithCredentials(accessKey, secretKey)
                              .WithSSL()
                              .Build();

            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("File is empty");

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var bucketName = "media";

                using (var stream = file.OpenReadStream())
                {
                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(fileName)
                        .WithStreamData(stream)
                        .WithContentType(file.ContentType);
                    await _minioClient.PutObjectAsync(putObjectArgs);
                }

                //var fileUrl = _minioClient.GetObjectUrl(bucketName, fileName);
                var fileUrl = $"{endpoint}/{bucketName}/{fileName}";

                return Ok(new { FileName = fileName, FileUrl = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

            //return Ok("Success");
        }
    }
}

