using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using VI.Base;
using VI.DB;
using VI.DB.Entities;
using QBM.CompositionApi.ApiManager;
using QBM.CompositionApi.Definition;
using QBM.CompositionApi.Crud;
using QER.CompositionApi.Portal;
using System.Xml.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Principal;
using NLog.Targets;
using VI.Base.Linq;
using System.Collections;
using System.Net;
using QBM.CompositionApi.Operations;


namespace QBM.CompositionApi
{
    // Class that implements an API provider for the portal
    public class MyApiEx3 : IApiProviderFor<QER.CompositionApi.Portal.PortalApiProject>, IApiProvider
    {
        // Method to define the API endpoint and its functionality
        public void Build(IApiBuilder builder)
        {
            // Define a new POST method for inserting identities
            builder.AddMethod(Method.Define("apiex3")
                .Handle<PostedID, ReturnedName>("POST", async (posted, qr, ct) =>
                {
                    var query = Query.From("AADGroup")
                        .Select("DisplayName", "UID_AADOrganization", "MailNickName", "Description")
                        .Where(string.Format(@"UID_AADGroup IN (
                            SELECT UID_AADGroup FROM AADGroup WHERE UID_AADGroup = '{0}'
                        )", posted.id));

                    var tryGet = await qr.Session.Source()
                        .TryGetAsync(query, EntityLoadType.DelayedLogic)
                        .ConfigureAwait(false);

                    // Convert the retrieved entity to a ReturnedName object and return it
                    return await ReturnedName.fromEntity(tryGet.Result, qr.Session)
                        .ConfigureAwait(false);
                }));

            builder.AddMethod(Method.Define("apiex3/insertAADGroup")
                .Handle<PostedIdentity, string>("POST", async (posted, qr, ct) =>
                {
                    string DisplayName = "";
                    string UID_AADOrganization = "";
                    string MailNickName = "";
                    string Description = "";

                    foreach (var column in posted.columns)
                    {
                        if (column.column == "DisplayName")
                        {
                            DisplayName = column.value;
                        }
                        if (column.column == "UID_AADOrganization")
                        {
                            UID_AADOrganization = column.value;
                        }
                        if (column.column == "MailNickName")
                        {
                            MailNickName = column.value;
                        }

                        if (column.column == "Description")
                        {
                            Description = column.value;
                        }
                    }
                    if (DisplayName.StartsWith("aad"))
                    {
                        var newID = await qr.Session.Source().CreateNewAsync("AADGroup",
                        new EntityParameters
                        {
                            CreationType = EntityCreationType.DelayedLogic
                        }, ct).ConfigureAwait(false);

                        await newID.PutValueAsync("DisplayName", DisplayName, ct).ConfigureAwait(false);
                        await newID.PutValueAsync("UID_AADOrganization", UID_AADOrganization, ct).ConfigureAwait(false);
                        await newID.PutValueAsync("MailNickName", MailNickName, ct).ConfigureAwait(false);
                        await newID.PutValueAsync("Description", Description, ct).ConfigureAwait(false);

                        using (var u = qr.Session.StartUnitOfWork())
                        {
                            await u.PutAsync(newID, ct).ConfigureAwait(false);  // Add the new entity to the unit of work
                            await u.CommitAsync(ct).ConfigureAwait(false);  // Commit the transaction to persist changes
                        }
                        return "successful creation";
                    }
                    else
                    {
                        return "false name";
                    }


                }));

            builder.AddMethod(Method.Define("apiex3/updateAADGroup")
                .Handle<PostedChangeIdentity, string>("POST", async (posted, qr, ct) =>
                {
                    var DisplayName = "";
                    var UID_AADOrganization = "";
                    var MailNickName = "";
                    var Description = "";

                    var query1 = Query.From("AADGroup")
                                      .Select("*")
                                      .Where(string.Format("UID_AADGroup = '{0}'", posted.id));

                    // Attempt to retrieve the entity asynchronously
                    var tryget = await qr.Session.Source()
                                       .TryGetAsync(query1, EntityLoadType.DelayedLogic, ct)
                                       .ConfigureAwait(false);


                    if (tryget.Success)
                    {
                        // Loop through each column in the posted data to update the entity's properties
                        foreach (var column in posted.columns)
                        {
                            // Assign values based on column names and update the entity accordingly
                            if (column.column == "DisplayName")
                            {
                                DisplayName = column.value.ToString();
                                await tryget.Result.PutValueAsync("DisplayName", DisplayName, ct).ConfigureAwait(false);
                            }
                            else if (column.column == "UID_AADOrganization")
                            {
                                UID_AADOrganization = column.value.ToString();
                                await tryget.Result.PutValueAsync("UID_AADOrganization", UID_AADOrganization, ct).ConfigureAwait(false);
                            }
                            else if (column.column == "MailNickName")
                            {
                                MailNickName = column.value.ToString();
                                await tryget.Result.PutValueAsync("MailNickName", MailNickName, ct).ConfigureAwait(false);
                            }
                            else if (column.column == "Description")
                            {
                                Description = column.value.ToString();
                                await tryget.Result.PutValueAsync("Description", Description, ct).ConfigureAwait(false);
                            }

                        }

                        using (var u = qr.Session.StartUnitOfWork())
                        {
                            // Add the updated entity to the unit of work
                            await u.PutAsync(tryget.Result, ct).ConfigureAwait(false);

                            // Commit the unit of work to persist changes
                            await u.CommitAsync(ct).ConfigureAwait(false);
                        }
                        return "update was successful";
                    }
                    else
                    {
                        return "the update failed";
                    }
                }));

            builder.AddMethod(Method.Define("apiex3/deleteAADGroup")
                .Handle<PostedID, string>("DELETE", async (posted, qr, ct) =>
                {
                    var id = posted.id;

                    var query1 = Query.From("AADGroup")
                                      .Select("*")
                                      .Where(string.Format("UID_AADGroup = '{0}'", id));

                    // Attempt to retrieve the entity from the database asynchronously
                    var tryget1 = await qr.Session.Source()
                                        .TryGetAsync(query1, EntityLoadType.DelayedLogic, ct)
                                        .ConfigureAwait(false);

                    // Check if the entity was successfully retrieved
                    if (tryget1.Success)
                    {
                        // Start a unit of work for transactional database operations
                        using (var u = qr.Session.StartUnitOfWork())
                        {
                            // Get the entity to be deleted
                            var objecttodelete = tryget1.Result;

                            // Mark the entity for deletion
                            objecttodelete.MarkForDeletion();

                            // Save the changes to the unit of work
                            await u.PutAsync(objecttodelete, ct).ConfigureAwait(false);

                            // Commit the unit of work to persist changes to the database
                            await u.CommitAsync(ct).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // If the entity was not found, return an error with a custom message and error code
                        return "No assignment was found";
                    }

                    // Return a successful response by converting the entity to ReturnedClass
                    return "successful delete";
                }));

            builder.AddMethod(Method.Define("apiex3/filters")
                .Handle<filters, List<ReturnedGroups>>("POST", async (posted, qr, ct) =>
                {
                    var query2 = Query.From("AADGroup").Select("*");
                    if (posted.UID_AADOrganization != null && posted.Description != null)
                    {
                        query2 = query2.Where(string.Format(" UID_AADOrganization= '{0}' and Description= '{1}'  ", posted.UID_AADOrganization, posted.Description));
                    }
                    else if (posted.Description == null)
                    {
                        query2 = query2.Where(string.Format(" UID_AADOrganization= '{0}'", posted.UID_AADOrganization));
                    }
                    else if(posted.UID_AADOrganization == null)
                    {
                        query2 = query2.Where(string.Format(" Description= '{0}'  ", posted.Description));

                    }
                    else {
                        return new List<ReturnedGroups>
                        {
                            new ReturnedGroups
                            {
                                DisplayName="no appropriate filter"
                            }
                        };
                    }


                    var CollGet = await qr.Session.Source()
                        .GetCollectionAsync(query2, EntityCollectionLoadType.Default, ct)
                        .ConfigureAwait(false);

                    System.Console.WriteLine(CollGet);


                    // Convert the collection of entities to a list of ReturnedNames objects and return
                    return await ReturnedGroups.fromEntity(CollGet, qr.Session).ConfigureAwait(false);


                }));




            builder.AddMethod(Method.Define("apiex3/pwo")
                .Handle<pworequest, string>("POST", async (posted, qr, ct) =>
                {
                    string uid_personOrdered = posted.username;
                    string uid_personinserted= qr.Session.User().Uid;
                    

                    var query3 = Query.From("AADGroup")
                                      .Select("*")
                                      .Where(string.Format("UID_AADGroup = '{0}'", posted.UID_AADGroup));

                    // Attempt to retrieve the entity asynchronously
                    var tryget3 = await qr.Session.Source()
                                       .TryGetAsync(query3, EntityLoadType.DelayedLogic, ct)
                                       .ConfigureAwait(false);
                    string accproduct= await tryget3.Result.GetValueAsync<string>("UID_AccProduct").ConfigureAwait(false);

                    var query4 = Query.From("ITShopOrg")
                                      .Select("*")
                                      .Where(string.Format("UID_AccProduct = '{0}'", accproduct));

                    // Attempt to retrieve the entity asynchronously
                    var tryget4 = await qr.Session.Source()
                                       .TryGetAsync(query4, EntityLoadType.DelayedLogic, ct)
                                       .ConfigureAwait(false);

                    string objectkeyordered = await tryget3.Result.GetValueAsync<string>("XObjectKey").ConfigureAwait(false);
                    string uid_org = await tryget4.Result.GetValueAsync<string>("UID_ITShopOrg").ConfigureAwait(false);

                    var newrequest = await qr.Session.Source().CreateNewAsync("PersonWantsOrg",
                        new EntityParameters
                        {
                            CreationType = EntityCreationType.DelayedLogic
                        }, ct).ConfigureAwait(false);

                    await newrequest.PutValueAsync("UID_PersonOrdered", uid_personOrdered, ct).ConfigureAwait(false);
                    await newrequest.PutValueAsync("UID_PersonInserted", uid_personinserted, ct).ConfigureAwait(false);
                    await newrequest.PutValueAsync("ObjectKeyOrdered", objectkeyordered, ct).ConfigureAwait(false);
                    await newrequest.PutValueAsync("UID_Org", uid_org, ct).ConfigureAwait(false);

                    using (var u = qr.Session.StartUnitOfWork())
                    {
                        await u.PutAsync(newrequest, ct).ConfigureAwait(false);  // Add the new entity to the unit of work
                        await u.CommitAsync(ct).ConfigureAwait(false);  // Commit the transaction to persist changes
                    }

                    



                    return "Success request!";

                }));

        }

        // Class to represent the posted data structure
        public class PostedID
        {
            public string id { get; set; }  // Array of columns containing data
        }

        public class ReturnedName
        {
            // Properties to hold the first name and last name of the user
            public string DisplayName { get; set; }
            public string UID_AADOrganization { get; set; }

            public string MailNickName { get; set; }


            public string Description { get; set; }

            // Static method to create a ReturnedName instance from an IEntity object
            public static async Task<ReturnedName> fromEntity(IEntity entity, ISession session)
            {
                // Instantiate a new ReturnedName object and populate it with data from the entity
                
                var g = new ReturnedName
                {
                    // Asynchronously get the FirstName value from the entity
                    DisplayName = await entity.GetValueAsync<string>("DisplayName").ConfigureAwait(false),

                    // Asynchronously get the LastName value from the entity
                    UID_AADOrganization = await entity.GetValueAsync<string>("UID_AADOrganization").ConfigureAwait(false),

                    MailNickName = await entity.GetValueAsync<string>("MailNickName").ConfigureAwait(false),

                    Description = await entity.GetValueAsync<string>("Description").ConfigureAwait(false),
                };

                // Return the populated ReturnedName object
                return g;
            }
        }

        public class PostedIdentity
        {
            public columnsarray[] columns;
        }

        public class filters
        {
            public string UID_AADOrganization;

            public string Description;
        }

        public class columnsarray
        {
            public string column { get; set; }
            public string value { get; set; }
        }

        public class PostedChangeIdentity
        {
            public string id { get; set; }

            public columnsarray[] columns { get; set; }
        }

        

       

        public class ReturnedGroups
        { 
            // Properties to hold personal information
            public string UID_AADGroup { get; set; }
            public string DisplayName { get; set; }
            public string UID_AADOrganization { get; set; }
            public string MailNickName { get; set; }

            public string Description { get; set; }

            // Static method to convert an IEntityCollection to a list of ReturnedNames objects
            public static async Task<List<ReturnedGroups>> fromEntity(IEntityCollection entityCollection, ISession session)
            {
                // Initialize a list to hold the ReturnedNames objects
                var groupList = new List<ReturnedGroups>();

                // Iterate over each entity in the collection
                foreach (var entity in entityCollection)
                {
                  
                    var g = new ReturnedGroups
                    {
                        // Get the first name from the entity
                        UID_AADGroup = await entity.GetValueAsync<string>("UID_AADGroup").ConfigureAwait(false),

                        // Get the last name from the entity
                        DisplayName = await entity.GetValueAsync<string>("DisplayName").ConfigureAwait(false),

                        UID_AADOrganization = await entity.GetValueAsync<string>("UID_AADOrganization").ConfigureAwait(false),

                        // Assign the department name retrieved earlier
                        MailNickName = await entity.GetValueAsync<string>("MailNickName").ConfigureAwait(false),

                        // Assign the head of department's internal name retrieved earlier
                        Description = await entity.GetValueAsync<string>("Description").ConfigureAwait(false),
                    };

                    // Add the ReturnedNames object to the list
                    groupList.Add(g);
                }

                // Return the list of ReturnedNames objects
                return groupList;
            }
        }

        public class pworequest
        {
            public string username;
            public string UID_AADGroup;
        }
    }
}