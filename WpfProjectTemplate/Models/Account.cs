using System.Security.Cryptography;
using System.Text;

namespace TextTool.Models
{
    internal class Account : EntityBase
    {
        private string _password;

        public string UserName { get; set; }

        public string Password
        {
            get => _password;
            set
            {
                using (var hasher = new SHA256CryptoServiceProvider())
                {
                    var hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(value));
                    _password = Encoding.ASCII.GetString(hash);
                }
            }
        }

        public bool VerifyPassword(string plain)
        {
            using (var hasher = new SHA256CryptoServiceProvider())
            {
                var hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(_password));
                return _password == Encoding.ASCII.GetString(hash);
            }
        }
    }
}