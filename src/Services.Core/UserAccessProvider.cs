﻿using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.Security;

namespace Compatibility.SenseNet.ContentRepository.Security
{
    public class UserAccessProvider : AccessProvider  //UNDONE:ODATA: ?? UserAccessProvider is not implemented
    {
        private IUser CurrentUser
        {
            get
            {
                //if (Thread.CurrentPrincipal?.Identity is IUser user)
                //    return user;

                //CurrentUser = StartupUser;
                //user = User.Administrator;
                //CurrentUser = user;
                //return user;
                throw new NotImplementedException();
            }
            set
            {
                //Thread.CurrentPrincipal = new PortalPrincipal(value);
                throw new NotImplementedException();
            }
        }

        public override IUser GetCurrentUser()
        {
            //if (HttpContext.Current == null)
            //    return CurrentUser;

            //IUser currentUser = null;
            //if (HttpContext.Current.User != null)
            //    currentUser = HttpContext.Current.User.Identity as IUser;

            //if (currentUser != null)
            //    return currentUser;

            //SetCurrentUser(StartupUser);
            //currentUser = StartupUser;

            //return currentUser;
            throw new NotImplementedException();
        }

        protected override void DoSetCurrentUser(IUser user)
        {
            //if (HttpContext.Current == null)
            //    CurrentUser = user;
            //else
            //    HttpContext.Current.User = new PortalPrincipal(user);
            throw new NotImplementedException();
        }

        public override bool IsAuthenticated
        {
            get
            {
                //if (HttpContext.Current == null)
                //    return false;

                //System.Security.Principal.IPrincipal currentPrincipal =
                //    HttpContext.Current.User;

                //return currentPrincipal?.Identity != null && currentPrincipal.Identity.IsAuthenticated;
                throw new NotImplementedException();
            }
        }
    }
}
