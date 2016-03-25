using SocialNetwork.App_Start;
using SocialNetwork.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SocialNetwork.Controllers
{

    public class HomeController : Controller
    {
        // GET: Home

        SocialNetworkDataContext dc = new SocialNetworkDataContext();

        public static string HashSHA1(string value)
        {
            var sha1 = System.Security.Cryptography.SHA1.Create();
            var inputBytes = Encoding.ASCII.GetBytes(value);
            var hash = sha1.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }


        [CheckNotifications]
        public ActionResult Feed()
        {


            if (Session["idUtilizador"] != null)
            {

                Utilizador c = dc.Utilizadors.Single(x => x.idUtilizador == Convert.ToInt32(Session["idUtilizador"]));

                ViewBag.UserPhoto = c.foto;
                ViewBag.idUtilizador = c.idUtilizador;
                List<Post> g = new List<Post>();

                var userGroups = (from gr in dc.Grupos
                                  from pg in dc.Perfil_Grupos
                                  where gr.idGrupo == pg.idGrupo &&
                                           pg.idUtilizador == Convert.ToInt32(Session["idUtilizador"])
                                  select gr.idGrupo).ToList();
                userGroups.Add(0);
                userGroups.Add(1);

                foreach (var post in dc.Posts)
                {
                    foreach (var ug in userGroups)
                    {
                        if (post.privacidade == Convert.ToInt32(ug) && post.visibilidade == true)
                            g.Add(post);
                    }

                }
                return View(g);



                // return View(dc.Posts);
            }
            else
            {
                // return RedirectToAction("Login");
                List<Post> l = new List<Post>();

                foreach (var post in dc.Posts)
                {
                    if (post.privacidade == 0 && post.visibilidade == true)
                        l.Add(post);
                }
                return View(l);
            }
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult Logout()
        {
            Session.RemoveAll();
            return RedirectToAction("Feed");
        }


        private ActionResult Index()
        {
            List<Post> g = new List<Post>();

            foreach (var post in dc.Posts)
            {
                if (post.privacidade == 0)
                    g.Add(post);
            }
            return View(g);

           // return View(dc.Posts);
        }

        [ChildActionOnly]
        [CheckLogin]
        public ActionResult NewPost()
        {
            var userGroups = (from gr in dc.Grupos
                              from pg in dc.Perfil_Grupos
                              where gr.idGrupo == pg.idGrupo &&
                                       pg.idUtilizador == Convert.ToInt32(Session["idUtilizador"])
                              select  gr.nome).ToList();
           userGroups.Add("Público");
            userGroups.Add("Registados");

            ViewBag.privacidade = new SelectList(userGroups);

            //ViewBag.privacidade = (ViewBag.privacidade).Reverse();

           var listaTipo = new List<string> { "Texto", "Imagem", "Vídeo", "Ficheiro" };

            ViewBag.tipo = new SelectList(listaTipo);

            //ViewBag.privacidade = new SelectList(userGroups, "idGrupo", "nome");


            //var foundItems = (from r in dc.Grupo where x.idGrupo = r.id select r).ToList(); since dc is datacontext;

            //    return View();
            //}

            //}
            return PartialView();
        }

        [CheckAuthentication]
        [HttpPost]
        public ActionResult NewPost(FormCollection collection, HttpPostedFileBase nome)
        {
            //ViewBag.privacidade = new SelectList(dc.Grupos, "ID", "Nome");

            

            //ViewBag.Something = new List<string> { "a", "b", "c" };

            if (ModelState.IsValid)
            {


                try
                {
                    Post novo = new Post();
                    //  novo.idCriador = Convert.ToInt32(collection["idCriador"]);
                    novo.idCriador = Convert.ToInt32(Session["idUtilizador"]);
                    novo.descricao = collection["descricao"];
                    novo.tipo = collection["tipo"];
                    novo.dataCriacao = DateTime.Now;
                    novo.visibilidade = true;
                    //var priv = collection["privacidade"];


                    if (collection["tipo"]== "Vídeo")
                    {
                        string video = collection["caminho"];
                        //char barra = '/';
                        //char igual = '=';
                        string[] idVideo;
                        idVideo = (video.Split('='));
                        if ( idVideo.Count() < 3 && idVideo.Count() != 1)
                        {
                            novo.caminho = "https://www.youtube.com/embed/" + idVideo[1];
                        }
                        else
                        {
                            idVideo = video.Split('/');
                            novo.caminho = "https://www.youtube.com/embed/" + idVideo[3];
                        }
                        
                        
                        
                    }
               


                    //var usr = dc.Utilizadors.Where(u => u.email == collection["email"] && u.pwd == collection["pwd"]).FirstOrDefault();

                    Grupo gr1 = dc.Grupos.Where(u => u.nome == collection["privacidade"]).FirstOrDefault();

                    //var privacidade = (from gr in dc.Grupos
                    //                   where gr.nome == priv
                    //                   select gr.idGrupo).ToList();

                    switch (collection["privacidade"])
                    {
                        case "Público":
                            novo.privacidade = 0;
                            break;
                        case "Registados":
                            novo.privacidade = 1;
                            break;

                        default:
                            novo.privacidade = gr1.idGrupo;
                            novo.idGrupo = gr1.idGrupo;
                            break;
                    }
                    //if (collection["privacidade"] == "Público")
                    //{
                    //    novo.privacidade = 0;
                    //}
                    //else
                    //{
                    //    novo.privacidade = gr1.idGrupo;
                    //}

                    if (nome != null){
                        //if (nome.ContentType != "image/png")
                        //    ModelState.AddModelError("nome", "Apenas ficheiros pdf");
                        //else
                        //{
                            string caminho = Server.MapPath(@"/Content/PostsAttachments");
                            try
                            {
                                nome.SaveAs(caminho + @"\" + Path.GetFileName(nome.FileName));
                                novo.caminho = "/Content/PostsAttachments/" + Path.GetFileName(nome.FileName);
                            }
                            catch (Exception)
                            {

                                ModelState.AddModelError("nome", "Erro no armazenamento do ficheiro");
                            }
                        }
                    //}


                    dc.Posts.InsertOnSubmit(novo);
                    dc.SubmitChanges();

                    return RedirectToAction("Feed");
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }
        }

        
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(FormCollection collection)
        {
            if (string.IsNullOrEmpty(collection["username"]))
                ModelState.AddModelError("username", "Tem de inserir um username");

            if (string.IsNullOrEmpty(collection["email"]))
                ModelState.AddModelError("email", "Tem de inserir um email");

            if (string.IsNullOrEmpty(collection["pwd"]))
                ModelState.AddModelError("pwd", "Tem de inserir uma password");

            var usr = dc.Utilizadors.Where(u => u.email == collection["email"]).FirstOrDefault();
            if (usr!=null)
                ModelState.AddModelError("email", "Já existe uma conta com este email!");


            if (ModelState.IsValid)
            {


                try
                {
                    Guid userGuid = System.Guid.NewGuid();

                    string hashedPassword = HashSHA1(collection["pwd"] + userGuid.ToString());

                    Utilizador novo = new Utilizador();
                    novo.username = collection["username"];
                    novo.email = collection["email"];
                    novo.pwd = hashedPassword;
                    novo.estado = 0; //Por Confirmar
                    novo.foto = "/Content/UserPhotos/fotoRedeSocial-SEMFOTO.png";
                    novo.tipo = "Normal";
                    novo.userGuid = userGuid;

                    dc.Utilizadors.InsertOnSubmit(novo);
                    dc.SubmitChanges();

                   // ModelState.Clear();
                   // ViewBag.Message = "Conta criada com sucesso!";
                    //return View();

                    // return RedirectToAction("Index");


                    System.Net.Mail.MailMessage m = new System.Net.Mail.MailMessage(
        new System.Net.Mail.MailAddress("yeahhmhmm@gmail.com", "YeahhMhmm!"),
        new System.Net.Mail.MailAddress(novo.email));
                    m.Subject = "Confirmação de conta";
                    m.Body = string.Format("Caro {0}. Obrigado pelo teu registo, por favor clica neste link para concluires o teu registo: {1}",
                    novo.username, Url.Action("ConfirmEmail", "Home", new { Token = novo.idUtilizador, Email = novo.email }, Request.Url.Scheme));
                    m.IsBodyHtml = true;
                    System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com");
                    smtp.Credentials = new System.Net.NetworkCredential("yeahhmhmm@gmail.com", "********");
                    smtp.EnableSsl = true;
                    smtp.Send(m);


                    return RedirectToAction("Confirm", "Home", new { Email = novo.email });


                    }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }
        }

        [CheckReferrer]
        public ActionResult Confirm(string Email)
        {
            ViewBag.Email = Email;
            return View();
        }

       
        public ActionResult Confirmed(string ConfirmedEmail)
        {
            ViewBag.Email = ConfirmedEmail;
            return View();
        }


        public ActionResult ConfirmEmail(string Token, string Email)
        {

            var user = dc.Utilizadors.Where(u => u.idUtilizador == Convert.ToInt32(Token)).FirstOrDefault();



            if (user != null)
            {
                if (user.email == Email && user.estado==0)
                {
                    user.estado = 1;
                    dc.SubmitChanges();
                    return RedirectToAction("Confirmed", "Home", new { ConfirmedEmail = user.email });
                }
                else
                {
                    return RedirectToAction("Confirm", "Home", new { Email = user.email });
                }
            }
            else
            {
                return RedirectToAction("Confirm", "Home", new { Email = "" });
            }
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(FormCollection collection)
        {

            if (string.IsNullOrEmpty(collection["email"]))
                ModelState.AddModelError("email", "Tem de inserir o email");

            if (string.IsNullOrEmpty(collection["pwd"]))
                ModelState.AddModelError("pwd", "Tem de inserir a password");

            //var usr = dc.Utilizadors.Where(u => u.email == collection["email"] && u.pwd == collection["pwd"]).FirstOrDefault();

            var usr = dc.Utilizadors.Where(u => u.email == collection["email"]).FirstOrDefault();

            


            if (usr == null)
            {
                ModelState.AddModelError("email", "Dados incorretos");
                //return RedirectToAction("Logout");
                return View();
            }

            if(usr.estado != 1)
            {
                    ModelState.AddModelError("email", "Conta desativada");
                    //return RedirectToAction("Logout");
                    return View();
            }

           

            if (ModelState.IsValid)
            {

                if (usr!=null)
            {

                string dbPassword = usr.pwd.ToString();
                string dbUserGuid = usr.userGuid.ToString();

                string hashedPassword = HashSHA1(collection["pwd"] + dbUserGuid);

                    if (dbPassword == hashedPassword)
                    {
                        Session["idUtilizador"] = usr.idUtilizador.ToString();
                        Session["username"] = usr.username.ToString();
                        Session["tipo"] = usr.tipo.ToString();
                        Session["estado"] = usr.estado;
                        if (usr.foto != null)
                        {
                            Session["foto"] = usr.foto.ToString();
                        }


                        return RedirectToAction("Feed");
                    }
                    else
                    {
                        ModelState.AddModelError("pwd", "Password errada!");
                    }
            }
            else
            {
                ModelState.AddModelError("", "O E-mail ou a Password está incorreto.");
            }
            return View();
            }
            else
            {
                return View();
            }
        }


        [ChildActionOnly]
        [CheckAuthentication]
        public ActionResult UserGroups()
        {
            Perfil_Grupo c = dc.Perfil_Grupos.Where(x => x.idUtilizador == Convert.ToInt32(Session["idUtilizador"])).FirstOrDefault();

            //if (c != null)
            //{
                List<Grupo> g = new List<Grupo>();
                foreach (var item in dc.Perfil_Grupos)
                {
                    if (item.idUtilizador== Convert.ToInt32(Session["idUtilizador"]) && item.Grupo.visibilidade==true)
                    {
                        Grupo gr = new Grupo();
                        Grupo gr2 = new Grupo();
                        gr.idGrupo = item.idGrupo;
                        gr2 = dc.Grupos.Where(x => x.idGrupo == gr.idGrupo).FirstOrDefault();
                        gr.nome = gr2.nome;

                        // Grupo gr = dc.Grupos.Where(x => x.idGrupo == item.idGrupo).FirstOrDefault();
                        //g.Add(item.idGrupo);
                        g.Add(gr);
                        
                    }
                    //g.Add(dc.Grupos.Single(x => x.idGrupo == Convert.ToInt32(c.idGrupo)));
                }
                return PartialView(g);

                //return PartialView("User_Groups",g);

                //return PartialView(dc.Grupos.Equals(g));
            //}

            //return PartialView(dc.Grupos);
        }


        //[CheckAuthentication]
        //public ActionResult GroupHeader()
        //{
        //    //var users = dc.Utilizadors.Where(x => x.idUtilizador == Perfil_Grupo.);

        //    var groupUsers = (from gr in dc.Grupos
        //                     from pg in dc.Perfil_Grupos
        //                     where gr.idGrupo == pg.idGrupo &&
        //                              gr.idGrupo == 2
        //                     select pg.idUtilizador).ToList();

        //    return View(groupUsers);

        //}

        [CheckIsInGroup(IdParamName = "id")]
        [CheckAuthentication]
        public ActionResult GroupPage(int id)
        {

            //Perfil_Grupo pertenceGrupo = dc.Perfil_Grupos.Where(x => x.idGrupo == id && x.idUtilizador == Convert.ToInt32(Session["idUtilizador"])).FirstOrDefault();

            //if (pertenceGrupo!=null)
            //{
            Grupo nomeGrupo = dc.Grupos.Single(x => x.idGrupo == id);

            if (nomeGrupo.visibilidade==false)
            {
                return RedirectToAction("Feed");
            }

            Utilizador c = dc.Utilizadors.Single(x => x.idUtilizador == Convert.ToInt32(Session["idUtilizador"]));

            Perfil_Grupo p = dc.Perfil_Grupos.Single(x => x.idUtilizador == Convert.ToInt32(Session["idUtilizador"]) && x.idGrupo==id);

            ViewBag.papel = p.papel;

            ViewBag.UserPhoto = c.foto;

            

            ViewBag.nomeGrupo = nomeGrupo.nome;

            ViewBag.idGrupo = nomeGrupo.idGrupo;

            List<Post> g = new List<Post>();
            foreach (var item in dc.Posts)
            {
                if (item.privacidade == id && item.visibilidade==true)
                    g.Add(item);
            }
                return View(g);

            //}
            //else
            //{
            //    return RedirectToAction("Feed");
            //}

        }

        [CheckAuthentication]
        public ActionResult NewGroup()
        {
            return View();
        }

        [CheckAuthentication]
        [HttpPost]
        public ActionResult NewGroup(FormCollection collection)
        {
            if (string.IsNullOrEmpty(collection["nome"]))
                ModelState.AddModelError("nome", "Tem de inserir um nome");

            var group = dc.Grupos.Where(u => u.nome == collection["nome"]).FirstOrDefault();


            if (group!=null)
                ModelState.AddModelError("nome", "Já existe um grupo com este nome");

            if (ModelState.IsValid)
            {


                try
                {
                   
                    Grupo novo = new Grupo();
                    novo.nome = collection["nome"];
                    novo.dataCriacao = DateTime.Now;
                    novo.visibilidade = true;
                    dc.Grupos.InsertOnSubmit(novo);
                    dc.SubmitChanges();

                    return RedirectToAction("MakeGroupAdmin",novo);
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }
        }

        [CheckReferrer]
        [CheckIsInGroup(IdParamName = "id")]
        [CheckAuthentication]
        public ActionResult EditGroup(int id)
        {
            var group = dc.Grupos.Where(u => u.idGrupo == id).FirstOrDefault();
            return View(group);
        }

        [CheckIsInGroup(IdParamName = "id")]
        [CheckAuthentication]
        [HttpPost]
        public ActionResult EditGroup(int id, FormCollection collection)
        {
            var group = dc.Grupos.Where(u => u.idGrupo == id).FirstOrDefault();
           
            var collectionVisibilidade = collection["visibilidade"];

            if (collectionVisibilidade != null)
            {
                group.visibilidade = false;
                group.nome = "---" + group.nome;

                foreach (Post item in dc.Posts)
                {
                    if (item.idGrupo == group.idGrupo)
                    {
                        item.visibilidade = false;
                    }
                }

                foreach (Perfil_Grupo item in dc.Perfil_Grupos)
                {
                    if (item.idGrupo == group.idGrupo)
                    {
                        dc.Perfil_Grupos.DeleteOnSubmit(item);
                    }
                }

                foreach (Convidar item in dc.Convidars)
                {
                    if (item.idGrupo == group.idGrupo)
                    {
                        item.estado = "Recusado";
                        //dc.Convidars.DeleteOnSubmit(item);
                    }
                }

                dc.SubmitChanges();
                return RedirectToAction("Feed");
            }

            group.nome = collection["nome"];
            dc.SubmitChanges();
            return RedirectToAction("GroupPage/" + id);
        }

        [CheckReferrer]
        [CheckIsInGroup(IdParamName = "id")]
        [CheckAuthentication]
        public ActionResult GroupMembers(int id)
        {
             var grupo = dc.Grupos.Where(u => u.idGrupo == id).FirstOrDefault();

            //var userGroups = (from gr in dc.Grupos
            //                  from pg in dc.Perfil_Grupos
            //                  from us in dc.Utilizadors
            //                  where gr.idGrupo == pg.idGrupo &&
            //                           gr.idGrupo == id
            //                  select us.idUtilizador).ToList();
            ViewBag.nomeGrupo = grupo.nome;
            ViewBag.idGrupo = grupo.idGrupo;

            var perfilGrupo = dc.Perfil_Grupos.Where(u => u.idGrupo == id && u.idUtilizador== Convert.ToInt32(Session["idUtilizador"])).FirstOrDefault();
            ViewBag.papel = perfilGrupo.papel;

            var groupUsers = (from gr in dc.Grupos
                              from pg in dc.Perfil_Grupos
                              from us in dc.Utilizadors
                              where gr.idGrupo == pg.idGrupo &&
                                       gr.idGrupo == id && us.idUtilizador==pg.idUtilizador
                              select us.idUtilizador).ToList();

            List<Utilizador> lista = new List<Utilizador>();

            foreach (var item in groupUsers)
            {
                var user = dc.Utilizadors.Where(u => u.idUtilizador == item).FirstOrDefault();
                lista.Add(user);
            }

            return View(lista);
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult RemoveFromGroup(int idUtilizador, int idGrupo)
        {
            //var grupo = dc.Grupos.Where(u => u.idGrupo == idGrupo).FirstOrDefault();
            //var user = dc.Utilizadors.Where(u => u.idUtilizador == idUtilizador).FirstOrDefault();
            var perfil = dc.Perfil_Grupos.Where(u => u.idUtilizador == idUtilizador && u.idGrupo == idGrupo).FirstOrDefault();
            dc.Perfil_Grupos.DeleteOnSubmit(perfil);
            dc.SubmitChanges();

            var perfilProprio = dc.Perfil_Grupos.Where(u => u.idUtilizador == Convert.ToInt32(Session["idUtilizador"]) && u.idGrupo == idGrupo).FirstOrDefault();
            if (perfilProprio !=null)
            {
                return RedirectToAction("GroupMembers/" + idGrupo);
            }
            return RedirectToAction("Feed");
        }

        [CheckReferrer]
        [CheckAuthentication]
        public string DeleteComment(int id)
        {

            Comentar comment = dc.Comentars.Single(u => u.idComentar == id);
            Post post = dc.Posts.Single(x => x.idPost == comment.idPost);
            comment.visibilidade = false;
            post.nrComentarios = post.nrComentarios - 1;
            dc.SubmitChanges();

            return " ";
            //return RedirectToAction("Feed");
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult MakeGroupAdmin(Grupo grupo)
        {
            Perfil_Grupo novo2 = new Perfil_Grupo();
           // Grupo gr1 = dc.Grupos.Where(u => u.dataCriacao == novo.dataCriacao).FirstOrDefault();
            novo2.idGrupo = grupo.idGrupo;
            novo2.idUtilizador = Convert.ToInt32(Session["idUtilizador"]);
            novo2.papel = "Admin";
            dc.Perfil_Grupos.InsertOnSubmit(novo2);
            dc.SubmitChanges();

            return RedirectToAction("Feed");
        }

        [CheckAuthentication]
        public ActionResult Settings()
        {
            var usr = dc.Utilizadors.Where(u => u.idUtilizador == Convert.ToInt32(Session["idUtilizador"])).FirstOrDefault();

            //Utilizador usr2 = dc.Utilizadors.Single(x => x.idUtilizador == id);
            return View(usr);
        }

        [CheckAuthentication]
        [HttpPost]
        public ActionResult Settings(FormCollection collection, HttpPostedFileBase nome)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Utilizador alterado = dc.Utilizadors.Where(u => u.idUtilizador == Convert.ToInt32(Session["idUtilizador"])).FirstOrDefault();
                    alterado.username = collection["username"];
                    alterado.email = collection["email"];
                    if (collection["pwd"] != alterado.pwd)
                    {
                        string hashedPassword = HashSHA1(collection["pwd"] + alterado.userGuid.ToString());
                        if (hashedPassword != alterado.pwd)
                        {
                            alterado.pwd = hashedPassword;
                        }
                    }
                    
                    

                    if (nome == null)
                        ModelState.AddModelError("nome", "Tem de selecionar um ficheiro");
                    else
                    {
                        if (nome.ContentType != "image/png" && nome.ContentType != "image/jpeg")
                            ModelState.AddModelError("nome", "Apenas ficheiros jpg ou png");
                        else
                        {
                            string caminho = Server.MapPath(@"/Content/UserPhotos");
                            try
                            {
                                 nome.SaveAs(caminho + @"\" + Path.GetFileName(nome.FileName));
                                alterado.foto = "/Content/UserPhotos/" + Path.GetFileName(nome.FileName);
                                Session["foto"]= "/Content/UserPhotos/" + Path.GetFileName(nome.FileName);
                            }
                            catch (Exception)
                            {

                                ModelState.AddModelError("nome", "Erro no armazenamento do ficheiro");
                            }
                        }
                    }
                    dc.SubmitChanges();
                    Session["username"] = collection["username"];
                    //ModelState.Clear();
                    ViewBag.Message = "Dados gravados com sucesso!";
                    return RedirectToAction("Settings");
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }   
        }

        [OnlyAjax]
        [CheckLogin]
        public string Like(int id)
        {
            Post a = dc.Posts.Single(x => x.idPost == id);
            Classificar c = dc.Classificars.Where(x => x.idUtilizador == Convert.ToInt32(Session["idUtilizador"]) && x.idPost == id && x.classificacao==true).FirstOrDefault();
           
                if (c== null)
            {
                a.gostos = a.gostos + 1;
               dc.SubmitChanges();
                Classificar novo = new Classificar();
                novo.idUtilizador = Convert.ToInt32(Session["idUtilizador"]);
                novo.idPost = a.idPost;
                novo.data = DateTime.Now;
                novo.classificacao = true;
                dc.Classificars.InsertOnSubmit(novo);
                dc.SubmitChanges();
            }
            else
            {
                a.gostos = a.gostos - 1;
                dc.SubmitChanges();

                dc.Classificars.DeleteOnSubmit(c);
                dc.SubmitChanges();
            }
            return a.gostos.ToString();
        }

        [OnlyAjax]
        [CheckLogin]
        public string Dislike(int id)
        {
            Post a = dc.Posts.Single(x => x.idPost == id);
            Classificar c = dc.Classificars.Where(x => x.idUtilizador == Convert.ToInt32(Session["idUtilizador"]) && x.idPost == id && x.classificacao == false).FirstOrDefault();

            if (c == null)
            {
                a.naoGostos = a.naoGostos + 1;
                dc.SubmitChanges();
                Classificar novo = new Classificar();
                novo.idUtilizador = Convert.ToInt32(Session["idUtilizador"]);
                novo.idPost = a.idPost;
                novo.data = DateTime.Now;
                novo.classificacao = false;
                dc.Classificars.InsertOnSubmit(novo);

                dc.SubmitChanges();
            }
            else
            {
                a.naoGostos = a.naoGostos - 1;
                dc.SubmitChanges();

                dc.Classificars.DeleteOnSubmit(c);
                dc.SubmitChanges();
            }
            return a.naoGostos.ToString();

        }

        [OnlyAjax]
        //[CheckAuthentication]
        public ActionResult PostDetails(int id)
        {
            ViewBag.idPost = id;

            return PartialView();
        }


        [CheckReferrer]
        //[CheckAuthentication]
        public ActionResult ListComments(int id)
        {
            ViewBag.idPost = id;

            return PartialView(dc.Comentars);
        }

        [CheckReferrer]
        //[CheckLogin]
        public ActionResult Comment(int id)
        {
            ViewBag.idPost = id;
            return PartialView();
        }

        [CheckReferrer]
        public string CommentCount(int id)
        {
            Post a = dc.Posts.Single(x => x.idPost == id);
            return a.nrComentarios.ToString();
        }

        [CheckReferrer]
        [CheckAuthentication]
        [HttpPost]
        public ActionResult Comment(FormCollection collection, int id)
        {
            ViewBag.idPost = id;
            if (ModelState.IsValid)
            {
                try
                {
                    Post a = dc.Posts.Single(x => x.idPost == id);
                    a.nrComentarios = a.nrComentarios + 1;
                    Comentar novo = new Comentar();
                    novo.idPost = id;
                    novo.idComentador = Convert.ToInt32(Session["idUtilizador"]);
                    novo.texto = collection["texto"];
                    novo.data = DateTime.Now;
                    novo.visibilidade = true;
                    dc.Comentars.InsertOnSubmit(novo);
                    dc.SubmitChanges();

                    return RedirectToAction("PostDetails/" + id);
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }
        }

        [CheckAuthentication]
        public ActionResult UserProfile(int id)
        {
            Utilizador user = dc.Utilizadors.Single(x => x.idUtilizador == id);
            if (user.estado !=1)
            {
                return RedirectToAction("Feed");
            }


            ViewBag.username = user.username;
            ViewBag.foto = user.foto;
            ViewBag.idUtilizador = user.idUtilizador;

            List<Post> g = new List<Post>();
            
            foreach (var item in dc.Posts)
            {
                if (item.idCriador == id && item.visibilidade == true)
                    g.Add(item);
            }

            var userGroups = (from gr in dc.Grupos
                              from pg in dc.Perfil_Grupos
                              where gr.idGrupo == pg.idGrupo &&
                                       pg.idUtilizador == Convert.ToInt32(Session["idUtilizador"])
                              select gr.idGrupo).ToList();

            var user2Groups = (from gr in dc.Grupos
                               from pg in dc.Perfil_Grupos
                               where gr.idGrupo == pg.idGrupo &&
                                        pg.idUtilizador == id
                               select gr.idGrupo).ToList();

            var grupos = userGroups.Intersect(user2Groups);
            List<Post> posts_comum = new List<Post>();

            foreach (var post in g)
            {
                foreach (var grupo in grupos)
                {
                    if (post.privacidade==grupo)
                    {
                        posts_comum.Add(post);
                    }
                }
            }

            foreach (var post in g)
            {
                if ((post.privacidade == 0 || post.privacidade == 1 ) && post.visibilidade==true)
                {
                    posts_comum.Add(post);
                }
            }
            var posts_ordenados = posts_comum.OrderBy(x => x.idPost).ToList();

            return View(posts_ordenados);
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult Invite(int id)
        {
            int userConvidado = id;

            var userGroups = (from gr in dc.Grupos
                              from pg in dc.Perfil_Grupos
                              where gr.idGrupo == pg.idGrupo &&
                                       pg.idUtilizador == Convert.ToInt32(Session["idUtilizador"])
                              select gr.nome).ToList();

            var user2Groups = (from gr in dc.Grupos
                              from pg in dc.Perfil_Grupos
                              where gr.idGrupo == pg.idGrupo &&
                                       pg.idUtilizador == userConvidado
                              select gr.nome).ToList();

            var grupos = userGroups.Except(user2Groups);

            ViewBag.idGrupo = new SelectList(grupos);
            return View();
        }

        [CheckAuthentication]
        [HttpPost]
        public ActionResult Invite(FormCollection collection, int id)
        {
            //string nomeGrupo = collection["idGrupo"];
            //if (nomeGrupo=="")
            //{
            //    // ModelState.AddModelError("idGrupo", "Tens de escolher um grupo");
            //    int userConvidado = id;

            //    var userGroups = (from gr in dc.Grupos
            //                      from pg in dc.Perfil_Grupos
            //                      where gr.idGrupo == pg.idGrupo &&
            //                               pg.idUtilizador == Convert.ToInt32(Session["idUtilizador"])
            //                      select gr.nome).ToList();

            //    var user2Groups = (from gr in dc.Grupos
            //                       from pg in dc.Perfil_Grupos
            //                       where gr.idGrupo == pg.idGrupo &&
            //                                pg.idUtilizador == userConvidado
            //                       select gr.nome).ToList();

            //    var grupos = userGroups.Except(user2Groups);

            //    ViewBag.idGrupo = new SelectList(grupos);
            //    TempData["Error"] = "Tens que escolher um grupo";
            //    return View();
            //}
           // return RedirectToAction("Error");

            if (ModelState.IsValid)
            {
                try
                {
                   // string nomeGrupo = collection["idGrupo"].ToString();

                    //if (nomeGrupo== null)
                    //{
                    //    return RedirectToAction("UserProfile/" + id);
                    //}
                    Convidar conv = new Convidar();
                    Grupo gr1 = dc.Grupos.Where(u => u.nome == collection["idGrupo"]).FirstOrDefault();
                    Convidar convite = dc.Convidars.Where(u => u.idGrupo == gr1.idGrupo && u.estado=="Espera" && u.idUtilizadorConvidado== id).FirstOrDefault();
                    if (convite!= null)
                    {
                        int userConvidado = id;

                        var userGroups = (from gr in dc.Grupos
                                          from pg in dc.Perfil_Grupos
                                          where gr.idGrupo == pg.idGrupo &&
                                                   pg.idUtilizador == Convert.ToInt32(Session["idUtilizador"])
                                          select gr.nome).ToList();

                        var user2Groups = (from gr in dc.Grupos
                                           from pg in dc.Perfil_Grupos
                                           where gr.idGrupo == pg.idGrupo &&
                                                    pg.idUtilizador == userConvidado
                                           select gr.nome).ToList();

                        var grupos = userGroups.Except(user2Groups);

                        ViewBag.idGrupo = new SelectList(grupos);
                        TempData["Error"] = "Utilizador já convidado";
                        return View();





                        //return RedirectToAction("Error");
                    }
                    conv.idGrupo = gr1.idGrupo;
                    conv.idUtilizadorConvida = Convert.ToInt32(Session["idUtilizador"]);
                    conv.idUtilizadorConvidado = id;
                    conv.data = DateTime.Now;
                    conv.estado = "Espera";
                    dc.Convidars.InsertOnSubmit(conv);
                    dc.SubmitChanges();

                    return RedirectToAction("UserProfile/"+id);
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }
        }

        [CheckAuthentication]
        [CheckNotifications]
        public ActionResult Notifications()
        {
            List<Convidar> c = new List<Convidar>();
            foreach (var convite in dc.Convidars)
            {
                if (convite.estado == "Espera" && convite.idUtilizadorConvidado == Convert.ToInt32(Session["idUtilizador"]))
                    c.Add(convite);
            }
            return View(c);
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult RefuseInvite(int id)
        {
            //Convidar conv = new Convidar();
            Convidar conv = dc.Convidars.Where(u => u.idConvite == id).FirstOrDefault();
            conv.estado = "Recusado";
            dc.SubmitChanges();

            return RedirectToAction("Notifications");
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult AcceptInvite(int id)
        {
            Convidar conv = dc.Convidars.Where(u => u.idConvite == id).FirstOrDefault();
            conv.estado = "Aceite";
            Perfil_Grupo novo2 = new Perfil_Grupo();
            novo2.idGrupo = conv.idGrupo;
            novo2.idUtilizador = Convert.ToInt32(Session["idUtilizador"]);
            novo2.papel = "Normal";
            dc.Perfil_Grupos.InsertOnSubmit(novo2);
            dc.SubmitChanges();

            return RedirectToAction("Notifications");
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult Download(string nome, int id)
        {
            string [] nomef = nome.Split('/');

            string caminho = Server.MapPath(@"/content/PostsAttachments");
            string caminhocompleto = caminho + @"\" + nomef[3];
            byte[] fileBytes = System.IO.File.ReadAllBytes(caminhocompleto);

            //if (Session["idUtilizador"]==null)
            //{
            //    return RedirectToAction("Login");
            //}

            if (System.IO.File.Exists(caminho + @"\" + nomef[3]))
            {
                Descarregar novo = new Descarregar();
                novo.idUtilizador = Convert.ToInt32(Session["idUtilizador"]);
                novo.idPost = id;
                novo.data = DateTime.Now;

                dc.Descarregars.InsertOnSubmit(novo);

                dc.SubmitChanges();
                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, nomef[3]);
            }
            ViewBag.xpto = nomef[3];
            return View("ErroDownload");
        }


        [CheckAuthentication]
        public ActionResult Search(string searchString)
        {
            if (String.IsNullOrEmpty(searchString))
            {
                return RedirectToAction("Feed");
            }
            var users = dc.Utilizadors.Where(s => s.username.Contains(searchString) && s.estado==1);
            return View(users);
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult AdminPanel()
        {
            Utilizador user = new Utilizador();

            if (Session["idUtilizador"]!=null)
            {
                user = dc.Utilizadors.Single(x => x.idUtilizador == Convert.ToInt32(Session["idUtilizador"]));
            }
            else
            {
                return RedirectToAction("Login");
            }

            if (user.tipo=="Admin")
            {
                return View(dc.Utilizadors);
            }
            return RedirectToAction("Login");
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult ReportedUsers()
        {
            Utilizador user = new Utilizador();

            if (Session["idUtilizador"] != null)
            {
                user = dc.Utilizadors.Single(x => x.idUtilizador == Convert.ToInt32(Session["idUtilizador"]));
            }
            else
            {
                return RedirectToAction("Login");
            }

            if (user.tipo == "Admin")
            {
                var denuncias = dc.Denuncia_Utilizadors.Where(s => s.estado.Contains("Em Avaliação"));
                return View(denuncias);
            }
            return RedirectToAction("Login");
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult ReportedPosts()
        {
            Utilizador user = new Utilizador();

            if (Session["idUtilizador"] != null)
            {
                user = dc.Utilizadors.Single(x => x.idUtilizador == Convert.ToInt32(Session["idUtilizador"]));
            }
            else
            {
                return RedirectToAction("Login");
            }

            if (user.tipo == "Admin")
            {
                var denuncias = dc.Denuncia_Posts.Where(s => s.estado.Contains("Em Avaliação"));
                return View(denuncias);
            }
            return RedirectToAction("Login");

        }


        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult ReportedGroups()
        {
            Utilizador user = new Utilizador();

            if (Session["idUtilizador"] != null)
            {
                user = dc.Utilizadors.Single(x => x.idUtilizador == Convert.ToInt32(Session["idUtilizador"]));
            }
            else
            {
                return RedirectToAction("Login");
            }

            if (user.tipo == "Admin")
            {
                var denuncias = dc.Denuncia_Grupos.Where(s => s.estado.Contains("Em Avaliação"));
                return View(denuncias);
            }
            return RedirectToAction("Login");

        }



        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult ReportUser(int id)
        {
            Utilizador user = dc.Utilizadors.Single(x => x.idUtilizador == id);
            ViewBag.username = user.username;

            return View();
        }


        [CheckAuthentication]
        [HttpPost]
        public ActionResult ReportUser(FormCollection collection, int id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Denuncia_Utilizador denuncia = new Denuncia_Utilizador();
                    Grupo gr1 = dc.Grupos.Where(u => u.nome == collection["idGrupo"]).FirstOrDefault();
                    denuncia.data = DateTime.Now;
                    denuncia.idUtilizadorQueixoso = Convert.ToInt32(Session["idUtilizador"]);
                    denuncia.idUtilizadorDenunciado = id;
                    denuncia.motivo = collection["motivo"];
                    denuncia.estado = "Em Avaliação";
                    dc.Denuncia_Utilizadors.InsertOnSubmit(denuncia);
                    dc.SubmitChanges();
                    return RedirectToAction("UserProfile/" + id);
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }

        }


        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult ReportGroup(int id)
        {
            Grupo grupo = dc.Grupos.Single(x => x.idGrupo == id);
            ViewBag.nome = grupo.nome;

            return View();
        }


        [CheckAuthentication]
        [HttpPost]
        public ActionResult ReportGroup(FormCollection collection, int id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Denuncia_Grupo denuncia = new Denuncia_Grupo();
                    //Grupo gr1 = dc.Grupos.Where(u => u.nome == collection["idGrupo"]).FirstOrDefault();
                    denuncia.data = DateTime.Now;
                    denuncia.idUtilizadorQueixoso = Convert.ToInt32(Session["idUtilizador"]);
                    denuncia.idGrupoDenunciado = id;
                    denuncia.motivo = collection["motivo"];
                    denuncia.estado = "Em Avaliação";
                    dc.Denuncia_Grupos.InsertOnSubmit(denuncia);
                    dc.SubmitChanges();
                    return RedirectToAction("GroupPage/" + id);
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }

        }


        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult DeletePost(int id)
        {
            Post post = dc.Posts.Single(x => x.idPost == id);
            post.visibilidade = false;
            dc.SubmitChanges();

            return RedirectToAction("Feed");
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult DeleteGroup(int id)
        {
            Grupo grupo = dc.Grupos.Single(x => x.idGrupo == id);
            grupo.visibilidade = false;
            grupo.nome = "---" + grupo.nome;

            foreach (Post item in dc.Posts)
            {
                if (item.idGrupo == grupo.idGrupo)
                {
                    item.visibilidade = false;
                }
            }

            foreach (Perfil_Grupo item in dc.Perfil_Grupos)
            {
                if (item.idGrupo == grupo.idGrupo)
                {
                    dc.Perfil_Grupos.DeleteOnSubmit(item);
                }
            }
            dc.SubmitChanges();

            return RedirectToAction("ReportedGroups");
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult ReportPost(int id)
        {
            //Utilizador user = dc.Utilizadors.Single(x => x.idUtilizador == id);
            //ViewBag.username = user.username;

            return View();
        }

        [CheckAuthentication]
        [HttpPost]
        public ActionResult ReportPost(FormCollection collection, int id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Denuncia_Post denuncia = new Denuncia_Post();
                    denuncia.data = DateTime.Now;
                    denuncia.idUtilizadorQueixoso = Convert.ToInt32(Session["idUtilizador"]);
                    denuncia.idPostDenunciado = id;
                    denuncia.motivo = collection["motivo"];
                    denuncia.estado = "Em Avaliação";
                    dc.Denuncia_Posts.InsertOnSubmit(denuncia);
                    dc.SubmitChanges();
                    return RedirectToAction("Feed");
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }

        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult EditUser(int id)
        {
            Utilizador user = dc.Utilizadors.Where(x => x.idUtilizador == id).FirstOrDefault();
            return View(user);
        }


        [CheckAuthentication]
        [HttpPost]
        public ActionResult EditUser(FormCollection collection, int id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Utilizador alterado = dc.Utilizadors.Single(u => u.idUtilizador == id);
                    alterado.username = collection["username"];
                    alterado.email = collection["email"];
                    if (collection["pwd"] != alterado.pwd)
                    {
                        string hashedPassword = HashSHA1(collection["pwd"] + alterado.userGuid.ToString());
                        if (hashedPassword != alterado.pwd)
                        {
                            alterado.pwd = hashedPassword;
                        }
                    }
                    alterado.estado = Convert.ToInt32(collection["estado"]);
                    alterado.foto = collection["foto"];
                    alterado.tipo = collection["tipo"];
                    dc.SubmitChanges();
                    return RedirectToAction("AdminPanel");
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }
        }

        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult BlockUser(int id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Utilizador user = dc.Utilizadors.Where(x => x.idUtilizador == id).FirstOrDefault();
                    user.estado = 666; //Bloqueado
                    dc.SubmitChanges();
                    return RedirectToAction("AdminPanel");
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }
        }


        [CheckReferrer]
        [CheckAuthentication]
        public ActionResult ArchivePostReport(int id)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    Denuncia_Post denuncia = dc.Denuncia_Posts.Single(x => x.idDenuncia == id);
                    denuncia.estado = "Arquivada";
                    dc.SubmitChanges();

                    return RedirectToAction("ReportedPosts");
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }
        }

        [CheckAuthentication]
        public ActionResult ArchiveUserReport(int id)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    Denuncia_Utilizador denuncia = dc.Denuncia_Utilizadors.Single(x => x.idDenuncia == id);
                    denuncia.estado = "Arquivada";
                    dc.SubmitChanges();

                    return RedirectToAction("ReportedUsers");
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }
        }

        public ActionResult ArchiveGroupReport(int id)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    Denuncia_Grupo denuncia = dc.Denuncia_Grupos.Single(x => x.idDenuncia == id);
                    denuncia.estado = "Arquivada";
                    dc.SubmitChanges();

                    return RedirectToAction("ReportedGroups");
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                return View();
            }
        }

    }
    }
