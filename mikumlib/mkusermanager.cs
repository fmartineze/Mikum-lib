using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace mikumlib
{
    public class Vouchers
    {
        public string Id { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Profile { get; set; }
        public Vouchers(string Id = "", string User = "", string Password = "", string Profile = "")
        {
            this.Id = Id;
            this.User = User;
            this.Password = Password;
            this.Profile = Profile;
        }
    }
    public class mkusermanager
    {

        //Private objs.
        Stream scon;
        TcpClient con;
        Random random = new Random((int)DateTime.Now.Ticks);

        //Public Properties
        public string server;
        public string user;
        public string password;
        public int port;



        //Constructors
        public mkusermanager() { }
        public mkusermanager(string server, int port, string user, string password)
        {
            server = this.server;
            port = this.port;
            user = this.user;
            password = this.password;
        }


        //Public Metods
        public bool Connect()  // Connect to Mikrotik Router
        {
            try
            {
                con = new TcpClient();
                con.Connect(server, port);
                scon = (Stream)con.GetStream();
            }
            catch
            {
                return false;
            }
            return true;
        }
        public bool IsConnected() // ¿ Is connected ?
        {
            return con.Connected;

        }
        public bool Login() // Login into Mikrotik
        {
            Send("/login", true);
            string hash = Read()[0].Split(new string[] { "ret=" }, StringSplitOptions.None)[1];
            Send("/login");
            Send("=name=" + user);
            Send("=response=00" + EncodePassword(password, hash), true);
            if (Read()[0] == "!done")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void Disconnect() // Disconnect from Mikrotik.
        {
            scon.Close();
            con.Close();
        }
        public void Send(string co, bool endsentence = false) // Send Sentence or Word
        {
            byte[] bytes = Encoding.ASCII.GetBytes(co.ToCharArray());
            byte[] bytes_len = EncodeLength(bytes.Length);
            scon.Write(bytes_len, 0, bytes_len.Length);
            scon.Write(bytes, 0, bytes.Length);
            if (endsentence) { scon.WriteByte(0); } // Send Zero Bytes end Sentence
        }
        public List<string> Read() // Read responses from Mikrotik
        {
            List<string> output = new List<string>();
            string o = "";
            byte[] tmp = new byte[4];
            long count;
            while (true)
            {
                tmp[3] = (byte)scon.ReadByte();

                if (tmp[3] == 0)
                {
                    output.Add(o);
                    if (o.Substring(0, 5) == "!done")
                    {
                        break;
                    }
                    else
                    {
                        o = "";
                        continue;
                    }
                }
                else
                {
                    if (tmp[3] < 0x80)
                    {
                        count = tmp[3];
                    }
                    else
                    {
                        if (tmp[3] < 0xC0)
                        {
                            int tmpi = BitConverter.ToInt32(new byte[] { (byte)scon.ReadByte(), tmp[3], 0, 0 }, 0);
                            count = tmpi ^ 0x8000;
                        }
                        else
                        {
                            if (tmp[3] < 0xE0)
                            {
                                tmp[2] = (byte)scon.ReadByte();
                                int tmpi = BitConverter.ToInt32(new byte[] { (byte)scon.ReadByte(), tmp[2], tmp[3], 0 }, 0);
                                count = tmpi ^ 0xC00000;
                            }
                            else
                            {
                                if (tmp[3] < 0xF0)
                                {
                                    tmp[2] = (byte)scon.ReadByte();
                                    tmp[1] = (byte)scon.ReadByte();
                                    int tmpi = BitConverter.ToInt32(new byte[] { (byte)scon.ReadByte(), tmp[1], tmp[2], tmp[3] }, 0);
                                    count = tmpi ^ 0xE0000000;
                                }
                                else
                                {
                                    if (tmp[3] == 0xF0)
                                    {
                                        tmp[3] = (byte)scon.ReadByte();
                                        tmp[2] = (byte)scon.ReadByte();
                                        tmp[1] = (byte)scon.ReadByte();
                                        tmp[0] = (byte)scon.ReadByte();
                                        count = BitConverter.ToInt32(tmp, 0);
                                    }
                                    else
                                    {
                                        break; //Error in receiving the packet, unknown length
                                    }
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    o += (Char)scon.ReadByte();
                }
            }
            return output;
        }

        public List<string> LoadProfiles() // Load Profiles from Mikrotik USER MANAGER.
        {
            List<string> tmplist = new List<string>();
            if (IsConnected())
            {
                Send("/tool/user-manager/profile/print", true); // Send print statement users
                int inicio = 0;
                foreach (string h in Read())
                {
                    inicio = h.IndexOf("=name=");
                    if (inicio > 0) tmplist.Add(h.Substring(inicio + 6, (h.IndexOf("=owner") - inicio) - 6));
                }
            }
            return tmplist;
        }
        public List<Vouchers> LoadVouchers(bool Clean_Vouchers = false) // Load VOUCHERS from Mikrotik USER MANAGER
        {
            List<Vouchers> TmpVouchers = new List<Vouchers>();
            if (IsConnected())
            {
                if (Clean_Vouchers)
                {
                    // Rebuild UserManager DATABASE
                    Send("/tool/user-manager/database/rebuild", true);
                    foreach (string h in Read()) { }
                }


                int inicio = 0;
                int largo = 0;
                string strid, strnombre, strperfil, strclave;
                Send("/tool/user-manager/user/print", true); // Send print statement users          
                foreach (string h in Read())
                {
                    inicio = h.IndexOf(".id=*");
                    if (inicio > 0 && h.IndexOf("=actual-profile=") > 0)
                    {
                        largo = h.IndexOf("=customer") - inicio;

                        strid = h.Substring(inicio + 5, largo - 5);
                        strnombre = h.Substring(h.IndexOf("=name=") + 6, h.IndexOf("=actual-profile=") - h.IndexOf("=name=") - 6);
                        strperfil = h.Substring(h.IndexOf("=actual-profile=") + 16, h.IndexOf("=password=") - h.IndexOf("=actual-profile=") - 16);
                        strclave = h.Substring(h.IndexOf("=password=") + 10, h.IndexOf("=shared-users") - h.IndexOf("=password=") - 10);

                        TmpVouchers.Add(new Vouchers(strid, strnombre, strclave, strperfil));
                    }
                    else if (inicio > 0 && h.IndexOf("=actual-profile=") < 0 && Clean_Vouchers) // Removes vouchers not assigned to a profile
                    {
                        strnombre = h.Substring(h.IndexOf("=name=") + 6, h.IndexOf("=password=") - h.IndexOf("=name=") - 6);
                        RemoveVoucher(strnombre);
                    }
                }

            }
            return TmpVouchers;
        }

        public Vouchers CreateVoucher(string Profile, int SizeUser, int SizePassword) // Create user with random strings
        {
            Vouchers TmpVoucher = new Vouchers();
            if (IsConnected())
            {
                bool UserExists = false;

                // New Random User
                while (true)
                {
                    TmpVoucher.User = RandomString(SizeUser);
                    TmpVoucher.Password = RandomString(SizePassword);

                    TmpVoucher.Profile = Profile;

                    Send("/tool/user-manager/user/add");
                    Send("=customer=admin");
                    Send("=name=" + TmpVoucher.User);
                    Send("=password=" + TmpVoucher.Password, true);   // New User
                    foreach (string h in Read())
                    {
                        if (h.IndexOf("message=failure") > 0) // Checks if the user already exists.
                        {
                            UserExists = true;
                        }
                        else
                        {
                            if (h.IndexOf("=ret=*") > 0)
                            {
                                UserExists = false;
                                TmpVoucher.Id = h.Substring(h.IndexOf("=ret=*") + 6, h.Length - (h.IndexOf("=ret=*") + 6));
                            }
                        }
                    }
                    if (!UserExists) { break; } // If the user does not exist, exit the loop.
                }

                // ASSIGN PROFILE
                Send("/tool/user-manager/user/create-and-activate-profile");
                Send("=customer=admin");
                Send("=profile=" + TmpVoucher.Profile);
                Send("=numbers=" + TmpVoucher.User, true);  // ASSIGN PROFILE
                foreach (string h in Read())
                {
                    if (h.IndexOf("input does not match") > 0) // Check that the profile exists
                    {
                        TmpVoucher.Profile = "ERROR";
                        RemoveVoucher(TmpVoucher.User); // Remove Voucher, Profile don't exists
                        TmpVoucher.User = "ERROR";
                        TmpVoucher.Password = "ERROR";
                    }
                }

            }
            return TmpVoucher;
        }
        public Vouchers CreateVoucher(string Profile, string User, string Password) // Create users with specific Strings
        {
            Vouchers TmpVoucher = new Vouchers();
            if (IsConnected())
            {
                bool UserExists = false;
                TmpVoucher.User = User;
                TmpVoucher.Password = Password;
                TmpVoucher.Profile = Profile;

                Send("/tool/user-manager/user/add");
                Send("=customer=admin");
                Send("=name=" + TmpVoucher.User);
                Send("=password=" + TmpVoucher.Password, true);   // New User
                foreach (string h in Read())
                {
                    if (h.IndexOf("message=failure") > 0) // Checks if the user already exists.
                    {
                        UserExists = true;
                        TmpVoucher.User = "ERROR";
                        TmpVoucher.Password = "ERROR";
                        return TmpVoucher;
                    }
                    else
                    {
                        if (h.IndexOf("=ret=*") > 0)
                        {
                            TmpVoucher.Id = h.Substring(h.IndexOf("=ret=*") + 6, h.Length - (h.IndexOf("=ret=*") + 6));
                        }
                    }
                }

                Send("/tool/user-manager/user/create-and-activate-profile");
                Send("=customer=admin");
                Send("=profile=" + TmpVoucher.Profile);
                Send("=numbers=" + TmpVoucher.User, true);  // assign profile
                foreach (string h in Read())
                {
                    if (h.IndexOf("input does not match") > 0) // Check that the profile exists
                    {
                        TmpVoucher.Profile = "ERROR";
                        RemoveVoucher(TmpVoucher.User); // Remove Voucher, Profile don't exists
                        TmpVoucher.User = "ERROR";
                        TmpVoucher.Password = "ERROR";
                    }
                }
            }
            return TmpVoucher;
        }

        public bool RemoveVoucher(string DelUser)
        {
            bool DeletedItem = false;
            if (IsConnected())
            {
                Send("/tool/user-manager/user/remove");
                Send("=numbers=" + DelUser, true);
                foreach (string h in Read())
                {
                    if (h.IndexOf("message=no such item") > 0) { DeletedItem = true; }
                }
            }
            return DeletedItem;
        }

        // Private Metods
        private byte[] EncodeLength(long valor) // Encode with byte mask and invert
        {
            if (valor < 0x80)
            {
                return new byte[1] { BitConverter.GetBytes(valor)[0] };
            }
            else if (valor < 0x4000)
            {
                byte[] tmp = BitConverter.GetBytes(valor | 0x8000);
                return new byte[2] { tmp[1], tmp[0] };
            }
            else if (valor < 0x200000)
            {
                byte[] tmp = BitConverter.GetBytes(valor | 0xC00000);
                return new byte[3] { tmp[2], tmp[1], tmp[0] };
            }
            else if (valor < 0x10000000)
            {
                byte[] tmp = BitConverter.GetBytes(valor | 0xE0000000);
                return new byte[4] { tmp[3], tmp[2], tmp[1], tmp[0] };
            }
            else
            {
                byte[] tmp = BitConverter.GetBytes(valor);
                return new byte[5] { 0xF0, tmp[3], tmp[2], tmp[1], tmp[0] };
            }
        }
        private string EncodePassword(string Password, string hash) // Encode Password to send to API
        {
            byte[] hash_byte = new byte[hash.Length / 2];
            for (int i = 0; i <= hash.Length - 2; i += 2)
            {
                hash_byte[i / 2] = Byte.Parse(hash.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
            }
            byte[] heslo = new byte[1 + Password.Length + hash_byte.Length];
            heslo[0] = 0;
            Encoding.ASCII.GetBytes(Password.ToCharArray()).CopyTo(heslo, 1);
            hash_byte.CopyTo(heslo, 1 + Password.Length);

            Byte[] hotovo;
            System.Security.Cryptography.MD5 md5;

            md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

            hotovo = md5.ComputeHash(heslo);

            //Convert encoded bytes back to a 'readable' string
            string navrat = "";
            foreach (byte h in hotovo)
            {
                navrat += h.ToString("x2");
            }
            return navrat;
        }
        private string RandomString(int size)  // Genera una cadena al azar
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 97)));
                builder.Append(ch);
            }
            return builder.ToString();
        }

    }
}
