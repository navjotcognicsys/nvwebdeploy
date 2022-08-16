using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using DocusignDemo.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;



namespace DocusignDemo.Controllers
{
    public class DocusignController : Controller
    {
        MyCredential credential = new MyCredential();
        private string INTEGRATOR_KEY = "7afd1821-38ac-410a-bed1-8bc109ecba2e";


        public ActionResult SendDocumentforSign()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SendDocumentforSign(DocusignDemo.Models.Recipient recipient, HttpPostedFileBase UploadDocument)
        {
            Models.Recipient recipientModel = new Models.Recipient();
            string directorypath = Server.MapPath("~/App_Data/" + "Files/");
            if (!Directory.Exists(directorypath))
            {
                Directory.CreateDirectory(directorypath);

            }

            byte[] data;
            using (Stream inputStream = UploadDocument.InputStream)
            {
                MemoryStream memoryStream = inputStream as MemoryStream;
                if (memoryStream == null)
                {
                    memoryStream = new MemoryStream();
                    inputStream.CopyTo(memoryStream);
                }
                data = memoryStream.ToArray();
            }

            var serverpath = directorypath + recipient.Name.Trim() + ".pdf";
            System.IO.File.WriteAllBytes(serverpath, data);
            docusign(serverpath, recipient.Name, recipient.Email);
            return View();
        }

        public string loginApi(string usr, string pwd)
        {
            // we set the api client in global config when we configured the client 
            ApiClient apiClient = new ApiClient("https://demo.docusign.net/restapi");
            string authHeader = "{\"Username\":\"" + usr + "\", \"Password\":\"" + pwd + "\", \"IntegratorKey\":\"" + INTEGRATOR_KEY + "\"}";
            apiClient.Configuration.DefaultHeader.Add("X-DocuSign-Authentication", authHeader);

            // we will retrieve this from the login() results
            string accountId = null;

            // the authentication api uses the apiClient (and X-DocuSign-Authentication header) that are set in Configuration object
            AuthenticationApi authApi = new AuthenticationApi(apiClient);
            LoginInformation loginInfo = authApi.Login();

            // find the default account for this user
            foreach (DocuSign.eSign.Model.LoginAccount loginAcct in loginInfo.LoginAccounts)
            {
                if (loginAcct.IsDefault == "true")
                {
                    accountId = loginAcct.AccountId;
                    break;
                }
            }
            if (accountId == null)
            { // if no default found set to first account
                accountId = loginInfo.LoginAccounts[0].AccountId;
            }
            return accountId;
        }

        public void docusign(string path, string recipientName, string recipientEmail)
        {

            ApiClient apiClient = new ApiClient("https://demo.docusign.net/restapi");
            //Configuration.Default.ApiClient = apiClient;


            //Verify Account Details
            string accountId = loginApi(credential.UserName, credential.Password);
            string authHeader = "{\"Username\":\"" + credential.UserName + "\", \"Password\":\"" + credential.Password + "\", \"IntegratorKey\":\"" + INTEGRATOR_KEY + "\"}";
            apiClient.Configuration.DefaultHeader.Add("X-DocuSign-Authentication", authHeader);

            // Read a file from disk to use as a document.
            byte[] fileBytes = System.IO.File.ReadAllBytes(path);

            EnvelopeDefinition envDef = new EnvelopeDefinition();
            envDef.EmailSubject = "Please sign this doc";

            // Add a document to the envelope
            Document doc = new Document();
            doc.DocumentBase64 = System.Convert.ToBase64String(fileBytes);
            doc.Name = Path.GetFileName(path);
            doc.DocumentId = "1";

            envDef.Documents = new List<Document>();
            envDef.Documents.Add(doc);

            // Add a recipient to sign the documeent
            DocuSign.eSign.Model.Signer signer = new DocuSign.eSign.Model.Signer();
            signer.Email = recipientEmail;
            signer.Name = recipientName;
            signer.RecipientId = "1";

            envDef.Recipients = new DocuSign.eSign.Model.Recipients();
            envDef.Recipients.Signers = new List<DocuSign.eSign.Model.Signer>();
            envDef.Recipients.Signers.Add(signer);

            //set envelope status to "sent" to immediately send the signature request
            envDef.Status = "sent";

            // |EnvelopesApi| contains methods related to creating and sending Envelopes (aka signature requests)
            EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);
            EnvelopeSummary envelopeSummary = envelopesApi.CreateEnvelope(accountId, envDef);

            // print the JSON response
            var result = JsonConvert.SerializeObject(envelopeSummary);
            Recipient recipient = new Recipient();
            recipient.Description = "envDef.EmailSubject";
            recipient.Email = recipientEmail;
            recipient.Name = recipientName;
            recipient.Status = envelopeSummary.Status;
            recipient.Documents = fileBytes;
            recipient.SentOn = System.Convert.ToDateTime(envelopeSummary.StatusDateTime);
            recipient.EnvelopeID = envelopeSummary.EnvelopeId;
            recipient.documentURL = envelopeSummary.Uri;
            DocuSignDemoEntities cSharpCornerEntities = new DocuSignDemoEntities();
            cSharpCornerEntities.Recipients.Add(recipient);
            cSharpCornerEntities.SaveChanges();
        }

        public ActionResult CreateEnvelopeFromCompositeTemplate()
        {
            var apiClient = new ApiClient("https://demo.docusign.net/restapi");
            //Configuration.Default.ApiClient = apiClient;

            MyCredential myCredential = new MyCredential();
            // call the Login() API which sets the user's baseUrl and returns their accountId
            string accountId = loginApi(myCredential.UserName, myCredential.Password);
            string authHeader = "{\"Username\":\"" + credential.UserName + "\", \"Password\":\"" + credential.Password + "\", \"IntegratorKey\":\"" + INTEGRATOR_KEY + "\"}";
            apiClient.Configuration.DefaultHeader.Add("X-DocuSign-Authentication", authHeader);
            EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);

            EnvelopeDefinition envelope = MakeCombineEnvelope("gyanendra.jbp@gmail.com", "gyanendra", "gyan@xemplarinsights.com","Gyan", "d8beefe4-fdb8-45a1-ad9f-19d224cf678d", "014b6e93-976a-4521-a79a-94d4fd945a33");
            EnvelopeSummary result = envelopesApi.CreateEnvelope(accountId, envelope);

            return RedirectToAction("ListDocuments", "Docusign");
        }
        private static byte[] document1(string signerEmail, string signerName, string ccEmail, string ccName,
            string item, string quantity)
        {
            // Data for this method
            // signerEmail 
            // signerName
            // ccEmail
            // ccName
            // item
            // quantity

            return Encoding.UTF8.GetBytes(" <!DOCTYPE html>\n" +
                    "    <html>\n" +
                    "        <head>\n" +
                    "          <meta charset=\"UTF-8\">\n" +
                    "        </head>\n" +
                    "        <body style=\"font-family:sans-serif;margin-left:2em;\">\n" +
                    "        <h1 style=\"font-family: 'Trebuchet MS', Helvetica, sans-serif;\n" +
                    "            color: darkblue;margin-bottom: 0;\">World Wide Corp</h1>\n" +
                    "        <h2 style=\"font-family: 'Trebuchet MS', Helvetica, sans-serif;\n" +
                    "          margin-top: 0px;margin-bottom: 3.5em;font-size: 1em;\n" +
                    "          color: darkblue;\">Order Processing Division</h2>\n" +
                    "        <h4>Ordered by " + signerName + "</h4>\n" +
                    "        <p style=\"margin-top:0em; margin-bottom:0em;\">Email: " + signerEmail + "</p>\n" +
                    "        <p style=\"margin-top:0em; margin-bottom:0em;\">Copy to: " + ccName + "," + ccEmail + "</p>\n" +
                    "        <p style=\"margin-top:3em; margin-bottom:0em;\">Item: <b>" + item + "</b>, quantity: <b>" + quantity + "</b> at market price.</p>\n" +
                    "        <p style=\"margin-top:3em;\">\n" +
                    "  Candy bonbon pastry jujubes lollipop wafer biscuit biscuit. Topping brownie sesame snaps sweet roll pie. Croissant danish biscuit soufflé caramels jujubes jelly. Dragée danish caramels lemon drops dragée. Gummi bears cupcake biscuit tiramisu sugar plum pastry. Dragée gummies applicake pudding liquorice. Donut jujubes oat cake jelly-o. Dessert bear claw chocolate cake gummies lollipop sugar plum ice cream gummies cheesecake.\n" +
                    "        </p>\n" +
                    "        <!-- Note the anchor tag for the signature field is in white. -->\n" +
                    "        <h3 style=\"margin-top:3em;\">Agreed: <span style=\"color:white;\">**signature_1**/</span></h3>\n" +
                    "        </body>\n" +
                    "    </html>");
        }
        private EnvelopeDefinition MakeCombineEnvelope(string signerEmail, string signerName, string ccEmail, string ccName, string templateId, string templateId1)
        {
            Text textInsteadOfNumber = new Text();
            textInsteadOfNumber.Value = "110011";
            textInsteadOfNumber.DocumentId = "1";
            textInsteadOfNumber.PageNumber = "1";
            textInsteadOfNumber.TabLabel = "numbersOnly";

            Text text = new Text();
            text.Value = "XemplarDemoByGyan";
            text.DocumentId = "1";
            text.PageNumber = "1";
            text.TabLabel = "text";

            List list1 = new List();
            list1.DocumentId = "1";
            list1.PageNumber = "1";
            list1.Value = "Red";
            list1.TabLabel = "list";

            RadioGroup radioG = new RadioGroup();
            radioG.GroupName = "radio1";
            radioG.DocumentId = "1";
            radioG.Radios = new List<Radio>
            {
                new Radio {PageNumber="1", Value="white",Selected="true"},
            };

            Checkbox check3 = new Checkbox();
            check3.DocumentId = "1";
            check3.PageNumber = "1";
            check3.TabLabel = "ckAgreement";
            check3.Selected = "true";
            Checkbox check4 = new Checkbox();
            check4.DocumentId = "1";
            check4.PageNumber = "1";
            check4.TabLabel = "ckAcknowledgement";
            check4.Selected = "true";

            List<Text> tabsTextList = new List<Text>();
            tabsTextList.Add(text);
            tabsTextList.Add(textInsteadOfNumber);

            List<List> tabsList = new List<List>();
            tabsList.Add(list1);

            List<RadioGroup> radioGroup = new List<RadioGroup>();
            radioGroup.Add(radioG);

            List<Checkbox> chkBox = new List<Checkbox>();
            chkBox.Add(check3);
            chkBox.Add(check4);

            Tabs tabs = new Tabs();
            tabs.TextTabs = tabsTextList;
            tabs.ListTabs = tabsList;
            tabs.RadioGroupTabs = radioGroup;
            tabs.CheckboxTabs = chkBox;
           

            ServerTemplate serverTemplate1 = new ServerTemplate
            {
                Sequence = "1",
                TemplateId = templateId
            };
            List<ServerTemplate> serverTemplates1 = new List<ServerTemplate> { serverTemplate1 };
            Signer signer1 = new Signer
            {
                Email = signerEmail,
                Name = signerName,
                RecipientId = "1",
                Tabs = tabs,
                RoleName = "signer"
            };
            CarbonCopy cc1 = new CarbonCopy
            {
                Email = ccEmail,
                Name = ccName,
                RoleName = "cc",
                RecipientId = "2"
            };
            List<Signer> signers1 = new List<Signer> { signer1 };
            Recipients recipients1 = new Recipients
            {
                Signers = signers1,
                CarbonCopies = new List<CarbonCopy> { cc1 }
            };
            InlineTemplate inlineTemplate1 = new InlineTemplate
            {
                Recipients = recipients1,
                Sequence = "1"
            };
            List<InlineTemplate> inlineTemplates1 = new List<InlineTemplate> { inlineTemplate1 };
            CompositeTemplate compositeTemplate1 = new CompositeTemplate
            {
                InlineTemplates = inlineTemplates1,
                ServerTemplates = serverTemplates1
            };
            ServerTemplate serverTemplate2 = new ServerTemplate
            {
                Sequence = "2",
                TemplateId = templateId1
            };
            List<ServerTemplate> serverTemplates2 = new List<ServerTemplate> { serverTemplate2 };
            Signer signer2 = new Signer
            {
                Email = signerEmail,
                Name = signerName,
                RecipientId = "1",
                RoleName = "signer"
            };

            CarbonCopy cc2 = new CarbonCopy
            {
                Email = ccEmail,
                Name = ccName,
                RoleName = "cc",
                RecipientId = "2"
            };
            List<Signer> signers2 = new List<Signer> { signer2 };
            Recipients recipients2 = new Recipients
            {
                Signers = signers2,
                CarbonCopies = new List<CarbonCopy> { cc2 }
            };
            InlineTemplate inlineTemplate2 = new InlineTemplate
            {
                Recipients = recipients2,
                Sequence = "2"
            };
            List<InlineTemplate> inlineTemplates2 = new List<InlineTemplate> { inlineTemplate2 };
            CompositeTemplate compositeTemplate2 = new CompositeTemplate
            {
                InlineTemplates = inlineTemplates2,
                ServerTemplates = serverTemplates2
            };
            List<CompositeTemplate> compositeTemplates1 = new List<CompositeTemplate> { compositeTemplate1, compositeTemplate2 };
            EnvelopeDefinition env = new EnvelopeDefinition
            {
                CompositeTemplates = compositeTemplates1,
                EmailBlurb = "Composite Templates",
                EmailSubject = "Merge Two document",
                Status = "sent"
            };
            return env;
        }

        public ActionResult CreateTemplate()
        {
            var apiClient = new ApiClient("https://demo.docusign.net/restapi");
           //Configuration.Default.ApiClient = apiClient;
           
            MyCredential myCredential = new MyCredential();
            // call the Login() API which sets the user's baseUrl and returns their accountId
            string accountId = loginApi(myCredential.UserName, myCredential.Password);
            string authHeader = "{\"Username\":\"" + credential.UserName + "\", \"Password\":\"" + credential.Password + "\", \"IntegratorKey\":\"" + INTEGRATOR_KEY + "\"}";
            apiClient.Configuration.DefaultHeader.Add("X-DocuSign-Authentication", authHeader);
            string templateId;
            string resultsTemplateName;

            TemplatesApi templatesApi = new TemplatesApi(apiClient);
            TemplatesApi.ListTemplatesOptions options = new TemplatesApi.ListTemplatesOptions();
            EnvelopeTemplate templateReqObject = MakeTemplate("XemplarDemoTemplate");
            TemplateSummary template = templatesApi.CreateTemplate(accountId, templateReqObject);

            // Retrieve the new template Name / TemplateId
            EnvelopeTemplateResults templateResults = templatesApi.ListTemplates(accountId, options);
            templateId = templateResults.EnvelopeTemplates[0].TemplateId;
            resultsTemplateName = templateResults.EnvelopeTemplates[0].Name;

            return RedirectToAction("ListDocuments", "Docusign");
        }
        public ActionResult SendEnvelopeFromTemplate()
        {
            var apiClient = new ApiClient("https://demo.docusign.net/restapi");
            //Configuration.Default.ApiClient = apiClient;

            MyCredential myCredential = new MyCredential();
            // call the Login() API which sets the user's baseUrl and returns their accountId
            string accountId = loginApi(myCredential.UserName, myCredential.Password);
            string authHeader = "{\"Username\":\"" + credential.UserName + "\", \"Password\":\"" + credential.Password + "\", \"IntegratorKey\":\"" + INTEGRATOR_KEY + "\"}";
            apiClient.Configuration.DefaultHeader.Add("X-DocuSign-Authentication", authHeader);
            EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);

            EnvelopeDefinition envelope = MakeEnvelope("gyanendra.jbp@gmail.com", "gyanendra", "gyan@xemplarinsights.com", "Gyan", "4c97ed07-3be1-4d5f-bd19-f867bd3a1a7f");
            EnvelopeSummary result = envelopesApi.CreateEnvelope(accountId, envelope);

            return RedirectToAction("ListDocuments", "Docusign");
        }

        private EnvelopeDefinition MakeEnvelope(string signerEmail, string signerName, string ccEmail, string ccName, string templateId)
        {
            EnvelopeDefinition env = new EnvelopeDefinition();
            env.TemplateId = templateId;

            TemplateRole signer1 = new TemplateRole();
            signer1.Email = signerEmail;
            signer1.Name = signerName;
            signer1.RoleName = "signer";
            Text textInsteadOfNumber = new Text();
            textInsteadOfNumber.Value = "110011";
            textInsteadOfNumber.DocumentId = "1";
            textInsteadOfNumber.PageNumber = "1";
            textInsteadOfNumber.TabLabel = "numbersOnly";

            Text text = new Text();
            text.Value = "XemplarDemoByGyan";
            text.DocumentId = "1";
            text.PageNumber = "1";
            text.TabLabel = "text";

            List list1 = new List();
            list1.DocumentId = "1";
            list1.PageNumber = "1";
            list1.Value = "Red";
            list1.TabLabel = "list";

            RadioGroup radioG = new RadioGroup();
            radioG.GroupName = "radio1";
            radioG.DocumentId = "1";
            radioG.Radios = new List<Radio>
            {
                new Radio {PageNumber="1", Value="white",Selected="true"},
            };

            Checkbox check3 = new Checkbox();
            check3.DocumentId = "1";
            check3.PageNumber = "1";
            check3.TabLabel = "ckAgreement";
            check3.Selected = "true";
            Checkbox check4 = new Checkbox();
            check4.DocumentId = "1";
            check4.PageNumber = "1";
            check4.TabLabel = "ckAcknowledgement";
            check4.Selected = "true";

            List<Text> tabsTextList = new List<Text>();
            tabsTextList.Add(text);
            tabsTextList.Add(textInsteadOfNumber);

            List<List> tabsList = new List<List>();
            tabsList.Add(list1);

            List<RadioGroup> radioGroup = new List<RadioGroup>();
            radioGroup.Add(radioG);

            List<Checkbox> chkBox = new List<Checkbox>();
            chkBox.Add(check3);
            chkBox.Add(check4);

            Tabs tabs = new Tabs();
            tabs.TextTabs = tabsTextList;
            tabs.ListTabs = tabsList;
            tabs.RadioGroupTabs = radioGroup;
            tabs.CheckboxTabs = chkBox;
            signer1.Tabs = tabs;
           

            TemplateRole cc1 = new TemplateRole();
            cc1.Email = ccEmail;
            cc1.Name = ccName;
            cc1.RoleName = "cc";

            env.TemplateRoles = new List<TemplateRole> { signer1, cc1 };
            env.Status = "sent";
            
            return env;
        }

        private static EnvelopeTemplate MakeTemplate(string resultsTemplateName)
        {
            Document doc = new Document();
            string docB64 = Convert.ToBase64String(System.IO.File.ReadAllBytes("D:\\Project\\SRM\\DocusignDemo\\DocusignDemo\\World_Wide_Corp_fields.pdf"));
            doc.DocumentBase64 = docB64;
            doc.Name = "Lorem Ipsum"; // can be different from actual file name
            doc.FileExtension = "pdf";
            doc.DocumentId = "1";

            // create a signer recipient to sign the document, identified by name and email
            // We're setting the parameters via the object creation
            Signer signer1 = new Signer();
            signer1.RoleName = "signer";
            signer1.RecipientId = "1";
            signer1.RoutingOrder = "1";
            // routingOrder (lower means earlier) determines the order of deliveries
            // to the recipients. Parallel routing order is supported by using the
            // same integer as the order for two or more recipients.

            // create a cc recipient to receive a copy of the documents, identified by name and email
            // We're setting the parameters via setters
            CarbonCopy cc1 = new CarbonCopy();
            cc1.RoleName = "cc";
            cc1.RoutingOrder = "2";
            cc1.RecipientId = "2";
            // Create fields using absolute positioning:
            SignHere signHere = new SignHere();
            signHere.DocumentId = "1";
            signHere.PageNumber = "1";
            signHere.XPosition = "191";
            signHere.YPosition = "148";

            Checkbox check1 = new Checkbox();
            check1.DocumentId = "1";
            check1.PageNumber = "1";
            check1.XPosition = "75";
            check1.YPosition = "417";
            check1.TabLabel = "ckAuthorization";

            Checkbox check2 = new Checkbox();
            check2.DocumentId = "1";
            check2.PageNumber = "1";
            check2.XPosition = "75";
            check2.YPosition = "447";
            check2.TabLabel = "ckAuthentication";

            Checkbox check3 = new Checkbox();
            check3.DocumentId = "1";
            check3.PageNumber = "1";
            check3.XPosition = "75";
            check3.YPosition = "478";
            check3.TabLabel = "ckAgreement";

            Checkbox check4 = new Checkbox();
            check4.DocumentId = "1";
            check4.PageNumber = "1";
            check4.XPosition = "75";
            check4.YPosition = "508";
            check4.TabLabel = "ckAcknowledgement";

            List list1 = new List();
            list1.DocumentId = "1";
            list1.PageNumber = "1";
            list1.XPosition = "142";
            list1.YPosition = "291";
            list1.Font = "helvetica";
            list1.FontSize = "size14";
            list1.TabLabel = "list";
            list1.Required = "false";
            list1.ListItems = new List<ListItem>
            {
                new ListItem {Text = "Red", Value = "Red"},
                new ListItem {Text = "Orange", Value = "Orange"},
                new ListItem {Text = "Yellow", Value = "Yellow"},
                new ListItem {Text = "Green", Value = "Green"},
                new ListItem {Text = "Blue", Value = "Blue"},
                new ListItem {Text = "Indigo", Value = "Indigo"},
                new ListItem {Text = "Violet", Value = "Violet"},
            };
            // The SDK can't create a number tab at this time. Bug DCM-2732
            // Until it is fixed, use a text tab instead.
            //   , number = docusign.Number.constructFromObject({
            //         documentId: "1", pageNumber: "1", xPosition: "163", yPosition: "260",
            //         font: "helvetica", fontSize: "size14", tabLabel: "numbersOnly",
            //         height: "23", width: "84", required: "false"})
            Text textInsteadOfNumber = new Text();
            textInsteadOfNumber.DocumentId = "1";
            textInsteadOfNumber.PageNumber = "1";
            textInsteadOfNumber.XPosition = "153";
            textInsteadOfNumber.YPosition = "260";
            textInsteadOfNumber.Font = "helvetica";
            textInsteadOfNumber.FontSize = "size14";
            textInsteadOfNumber.TabLabel = "numbersOnly";
            textInsteadOfNumber.Height = "23";
            textInsteadOfNumber.Width = "84";
            textInsteadOfNumber.Required = "false";

            RadioGroup radioGroup = new RadioGroup();
            radioGroup.DocumentId = "1";
            radioGroup.GroupName = "radio1";

            radioGroup.Radios = new List<Radio>
            {
                new Radio {PageNumber="1", Value="white", XPosition="142", YPosition="384", Required = "false"},
                new Radio {PageNumber="1", Value="red", XPosition="74", YPosition="384", Required = "false"},
                new Radio {PageNumber="1", Value="blue", XPosition="220", YPosition="384", Required = "false"}
            };

            Text text = new Text();
            text.DocumentId = "1";
            text.PageNumber = "1";
            text.XPosition = "153";
            text.YPosition = "230";
            text.Font = "helvetica";
            text.FontSize = "size14";
            text.TabLabel = "text";
            text.Height = "23";
            text.Width = "84";
            text.Required = "false";

            // Tabs are set per recipient / signer
            Tabs signer1Tabs = new Tabs();
            signer1Tabs.CheckboxTabs = new List<Checkbox>
            {
                check1, check2, check3, check4
            };

            signer1Tabs.ListTabs = new List<List> { list1 };
            // numberTabs: [number],
            signer1Tabs.RadioGroupTabs = new List<RadioGroup> { radioGroup };
            signer1Tabs.SignHereTabs = new List<SignHere> { signHere };
            signer1Tabs.TextTabs = new List<Text> { text, textInsteadOfNumber };

            signer1.Tabs = signer1Tabs;

            // Add the recipients to the env object
            Recipients recipients = new Recipients();
            recipients.Signers = new List<Signer> { signer1 };
            recipients.CarbonCopies = new List<CarbonCopy> { cc1 };


            // create the overall template definition
            EnvelopeTemplate template = new EnvelopeTemplate();
            // The order in the docs array determines the order in the env
            template.Description = "Example template created via the API";
            template.Name = resultsTemplateName;
            template.Documents = new List<Document> { doc };
            template.EmailSubject = "Please sign this document";
            template.Recipients = recipients;
            template.Status = "created";

            return template;
        }

        public ActionResult getEnvelopeInformation()
        {
            ApiClient apiClient = new ApiClient("https://demo.docusign.net/restapi");
            //Configuration.Default.ApiClient = apiClient;

            // provide a valid envelope ID from your account.  
            MyCredential myCredential = new MyCredential();

            // call the Login() API which sets the user's baseUrl and returns their accountId
            string accountId = loginApi(myCredential.UserName, myCredential.Password);
            string authHeader = "{\"Username\":\"" + credential.UserName + "\", \"Password\":\"" + credential.Password + "\", \"IntegratorKey\":\"" + INTEGRATOR_KEY + "\"}";
            apiClient.Configuration.DefaultHeader.Add("X-DocuSign-Authentication", authHeader);
            //===========================================================
            // Step 2: Get Envelope Information
            //===========================================================

            // |EnvelopesApi| contains methods related to creating and sending Envelopes including status calls

            DocusignDemo.Models.DocuSignDemoEntities cDocuSignDemoEntities = new DocusignDemo.Models.DocuSignDemoEntities();
            var recipients = cDocuSignDemoEntities.Recipients.ToList();
            EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);
           
            foreach (var recipient in recipients)
            {
                Envelope envInfo = envelopesApi.GetEnvelope(accountId, recipient.EnvelopeID);

                if (envInfo.Status == "completed")
                {
                    //recipient = cDocuSignDemoEntities.Recipients.Where(a => a.EnvelopeID == envelopeId).FirstOrDefault();
                    recipient.Status = "completed";
                    recipient.UpdatedOn = System.DateTime.Now;
                    cDocuSignDemoEntities.Entry(recipient).State = EntityState.Modified;
                    cDocuSignDemoEntities.SaveChanges();
                }
            }

            //   EnvelopesApi envelopesApi = new EnvelopesApi();
            // Envelope envInfo = envelopesApi.GetEnvelope(accountId, envelopeId);
            //if (envInfo.Status == "completed")
            //{
            //    DocusignDemo.Models.DocuSignDemoEntities cDocuSignDemoEntities = new DocusignDemo.Models.DocuSignDemoEntities();
            //    var recipient = cDocuSignDemoEntities.Recipients.Where(a => a.EnvelopeID == envelopeId).FirstOrDefault();
            //    recipient.Status = "completed";
            //    recipient.UpdatedOn = System.DateTime.Now;
            //    cDocuSignDemoEntities.Entry(recipient).State = EntityState.Modified;
            //    cDocuSignDemoEntities.SaveChanges();
            //}
            return RedirectToAction( "ListDocuments", "Docusign");

        } // end requestSignatu

        List<DocusignDemo.Models.Recipient> recipientsDocs = new List<DocusignDemo.Models.Recipient>();
        public ActionResult ListDocuments()
        {

            ApiClient apiClient = new ApiClient("https://demo.docusign.net/restapi");
            //Configuration.Default.ApiClient = apiClient;
            MyCredential myCredential = new MyCredential();

            // call the Login() API which sets the user's baseUrl and returns their accountId
            string accountId = loginApi(myCredential.UserName, myCredential.Password);
            string authHeader = "{\"Username\":\"" + credential.UserName + "\", \"Password\":\"" + credential.Password + "\", \"IntegratorKey\":\"" + INTEGRATOR_KEY + "\"}";
            apiClient.Configuration.DefaultHeader.Add("X-DocuSign-Authentication", authHeader);

            DocusignDemo.Models.DocuSignDemoEntities cDocuSignDemoEntities = new DocusignDemo.Models.DocuSignDemoEntities();
            var recipients = cDocuSignDemoEntities.Recipients.ToList();
            string serverDirectory = Server.MapPath("~/Uploadfiles/");
            if (!Directory.Exists(serverDirectory))
            {
                Directory.CreateDirectory(serverDirectory);
            }

            foreach (var recipient in recipients)
            {
                string recipientDirectory = Server.MapPath("~/Uploadfiles/" + recipient.EnvelopeID);

                if (!Directory.Exists(recipientDirectory))
                {
                    Directory.CreateDirectory(recipientDirectory);
                }

                EnvelopeDocumentsResult documentList = ListEnvelopeDocuments(accountId, recipient.EnvelopeID);

                int i = 0;
                string SignedPDF = string.Empty;
                EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);
                foreach (var document in documentList.EnvelopeDocuments)
                {
                    string signingStatus = recipient.Status == "completed" ? "Signed" : "Yet to Sign";
                    MemoryStream docStream = (MemoryStream)envelopesApi.GetDocument(accountId, recipient.EnvelopeID, documentList.EnvelopeDocuments[i].DocumentId);
                    string documentName = document.Name != "Summary" ? document.Name : "Summary";
                    SignedPDF = Server.MapPath("~/Uploadfiles/" + recipient.EnvelopeID + "/" + recipient.EnvelopeID + "_" + documentName + ".pdf");
                    using (var fileStream = System.IO.File.Create(SignedPDF))
                    {
                        docStream.Seek(0, SeekOrigin.Begin);
                        docStream.CopyTo(fileStream);
                    }
                    var authorityURL = "http://" + HttpContext.Request.Url.Authority + "/Uploadfiles/" + recipient.EnvelopeID + "/" + recipient.EnvelopeID + "_" + documentName + ".pdf";

                    recipientsDocs.Add(new DocusignDemo.Models.Recipient
                    {
                        EnvelopeID = recipient.EnvelopeID,
                        Name = recipient.Name,
                        Email = recipient.Email,
                        Status = signingStatus,
                        documentURL = SignedPDF,
                        SentOn = recipient.SentOn,
                        UpdatedOn = recipient.UpdatedOn,
                        ValidateDoc = authorityURL
                    });

                    i++;
                }
            }

            return View(recipientsDocs);
        }

        public EnvelopeDocumentsResult ListEnvelopeDocuments(string accountId, string envelopeId)
        {
            ApiClient apiClient = new ApiClient("https://demo.docusign.net/restapi");
            string authHeader = "{\"Username\":\"" + credential.UserName + "\", \"Password\":\"" + credential.Password + "\", \"IntegratorKey\":\"" + INTEGRATOR_KEY + "\"}";
            apiClient.Configuration.DefaultHeader.Add("X-DocuSign-Authentication", authHeader);
            EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);
            EnvelopeDocumentsResult docsList = envelopesApi.ListDocuments(accountId, envelopeId);
            return docsList;
        }

        public FileResult Download(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string contentType = "application/pdf";
            return File(filePath, contentType, fileName);
        }
    }

    public class MyCredential
    {
        public string UserName { get; set; } = "gyan@xemplarinsights.com";
        public string Password { get; set; } = "Gy@nendra1"; // Need to add password
    }

}
