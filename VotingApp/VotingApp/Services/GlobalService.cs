﻿using AutoMapper;
using CoderCamps.Data.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VotingApp.Domain.Models;
using VotingApp.Services.Models;

namespace VotingApp.Services {
    public class GlobalService {

        private IRepository _repo;

        public GlobalService(IRepository repo) {
            _repo = repo;
        }

        public AddOrUpdate(GlobalAdminDTO global) {
            if (global.Id == null) {
                _repo.Add<GlobalAdmin>(Mapper.Map<GlobalAdmin>(global));
            }
            else {
                
                var dbGlobalAdmin = Mapper.Map<List<GlobalAdminDTO>>(from ga in _repo.Query<GlobalAdmin>() where ga.Id == global.Id select ga).FirstOrDefault();
                return Mapper.Map<GlobalAdminDTO>(dbGlobalAdmin);
            }
            _repo.SaveChanges();
            return Mapper.Map<GlobalAdminDTO>(dbGlobalAdmin);
        }
        public GlobalService(IRepository repo)
        {
            _repo = repo;
        }

        public IList<GlobalAdminDTO> List()
        {
            return Mapper.Map<List<GlobalAdminDTO>>((from ai in _repo.Query<Global>()
                                               select ai).ToList());
        }

        public GlobalAdminDTO Find(int id)
        {
            return Mapper.Map<GlobalAdminDTO>(FindInternal(id));
        }

        private GlobalAdmin FindInternal(int id)
        {
            return (from ai in _repo.Query<GlobalAdmin>()
                    where ai.Id == id
                    select ai).FirstOrDefault();
        }

        public void Add(GlobalAdmin item)
        {
            _repo.Add(Mapper.Map<GlobalAdmin>(item));
            _repo.SaveChanges();
        }

        public void Update(GlobalAdmin item)
        {

            var dbItem = FindInternal(item.Id);

            Mapper.Map(item, dbItem);
            //dbItem.Name = item.Name;
            //dbItem.Description = item.Description;
            //dbItem.MaxNumberOfBids = item.MaxNumberOfBids;
            //dbItem.MinimumBid = item.MinimumBid;
            _repo.SaveChanges();
        }

        public void Delete(int id)
        {
            var dbGlobalAdmin = FindInternal(id);
            _repo.Delete(dbGlobalAdmin);
            _repo.SaveChanges();
        }
    }
}