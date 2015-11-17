﻿using AutoMapper;
using CoderCamps.Data.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using VotingApp.Domain.Models;
using VotingApp.Services.Models;

namespace VotingApp.Services
{
    //[Authorize(Roles="Director, Chairman")]
    public class MotionService
    {
        private IRepository _repo;

        public MotionService(IRepository repo)
        {
            _repo = repo;
        }

        public IList<MotionDTO> List()
        {
            var dbMotions = (from m in _repo.Query<Motion>().Include(m => m.Votes).Include(m => m.Comments)
                             select m).ToList();
            return Mapper.Map<List<MotionDTO>>(dbMotions);
        }


        public MotionDTO Find(int id)
        {
            return Mapper.Map<MotionDTO>(_repo.Find<Motion>(id));
        }

        //[Authorize(Roles = "Active")]
        public void AddOrUpdate(MotionDTO motion, string Id)
        {
            if (motion.Id == 0)
            {
                motion.Active = true;
                motion.AllowSecond = false;
                motion.CreatedById = Id;
                motion.DateCreated = DateTime.Now;
                _repo.Add(Mapper.Map<Motion>(motion));
            }
            else
            {
                var dbMotion = FindInternal(motion.Id);
                motion.WasEdited = (motion.WasEdited ? motion.WasEdited : true);
                Mapper.Map(motion, dbMotion);
            }
            _repo.SaveChanges();
        }

        public string FindCurrentUser(string username)
        {
            return (from u in _repo.Query<ApplicationUser>()
                    where u.UserName == username
                    select u.Id).FirstOrDefault();
        }

        private Motion FindInternal(int id)
        {
            return (from m in _repo.Query<Motion>()
                    where m.Id == id
                    select m).FirstOrDefault();
        }

        //[Authorize(Roles = "Active")]
        public MotionDTO Update(MotionDTO motion)
        {
            var dbMotion = FindInternal(motion.Id);

            Mapper.Map(motion, dbMotion);
            _repo.SaveChanges();
            return Mapper.Map<MotionDTO>(dbMotion);
        }

        //[Authorize(Roles = "Director")]
        //[Authorize(Roles = "Active")]
        public void SecondMotion(MotionDTO motion, string Id)
        {
            motion.Seconded = true;
            motion.DateSeconded = DateTime.Now;
            motion.SecondedById = Id;
            _repo.Add(Mapper.Map<Motion>(motion));
            _repo.SaveChanges();
        }

        //[Authorize(Roles = "Chairman")]
        //[Authorize(Roles = "Active")]
        public void AllowSecond(MotionDTO motion)
        {
            motion.AllowSecond = true;
            _repo.Add(Mapper.Map<Motion>(motion));
            _repo.SaveChanges();
        }

        //[Authorize(Roles = "Chairman")]
        //[Authorize(Roles = "Active")]
        public void KillMotion(MotionDTO motion, CommentDTO reason)
        {
            motion.Comments.Add(reason);
            motion.AllowSecond = true;
            motion.DateResult = DateTime.Now;
            motion.Active = false;
            _repo.Add(Mapper.Map<Motion>(motion));
            _repo.SaveChanges();
        }



    }
}