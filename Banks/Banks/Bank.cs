﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Banks.Accounts;
using Banks.Clients;
using Banks.Exceptions;
using Banks.Messages;
using Banks.Transactions;

namespace Banks.Banks
{
    public class Bank
    {
        private static int _banksId = 1;

        private static int _accountsCounter = 1;

        private readonly List<IAccount> _observers;

        private readonly List<IAccount> _accounts;

        private readonly double _percent;

        private readonly double _commission;

        private readonly double _creditLimit;

        private readonly double _maxTransfer;

        private readonly double _maxWithdraw;

        private readonly Dictionary<double, double> _percentsBorders;

        public Bank(
            string name,
            double percent,
            double commission,
            double creditLimit,
            double maxWithdraw,
            double maxTransfer,
            Dictionary<double, double> percentsBorders)
        {
            Id = _banksId++;
            Name = name;
            _percent = percent;
            _commission = commission;
            _creditLimit = creditLimit;
            _maxWithdraw = maxWithdraw;
            _maxTransfer = maxTransfer;
            _percentsBorders = percentsBorders;
            _accounts = new List<IAccount>();
            _observers = new List<IAccount>();
        }

        public int Id { get; }

        public string Name { get; }

        public void RegisterObserver(IAccount observer)
        {
            if (_observers.Any(obs => obs == observer))
            {
                throw new AlreadyRegisteredObserverException();
            }

            _observers.Add(observer);
        }

        public void RemoveObserver(IAccount observer)
        {
            if (_observers.All(obs => obs != observer))
            {
                throw new NotRegisteredObserverException();
            }

            _observers.Remove(observer);
        }

        public void SendNotify(List<IAccount> observers, double amount, IBankMessage message)
        {
            foreach (IAccount observer in observers)
            {
                observer.Update(message.Message(amount));
            }
        }

        public IAccount CreateDebitAccount(Person person, double startBalance)
        {
            var account = new DebitAccount(
                _accountsCounter++,
                _percent,
                _maxTransfer,
                _maxWithdraw,
                startBalance);

            _accounts.Add(account);
            person.AddNewAccount(account);

            return account;
        }

        public IAccount CreateDepositAccount(Person person, double startBalance, DateTime end)
        {
            var account = new DepositAccount(
                _accountsCounter++,
                ChooseDepositPercent(startBalance),
                _maxTransfer,
                _maxWithdraw,
                startBalance,
                end);

            _accounts.Add(account);
            person.AddNewAccount(account);

            return account;
        }

        public IAccount CreateCreditAccount(Person person, double startBalance)
        {
            var account = new CreditAccount(
                _accountsCounter++,
                _maxTransfer,
                _maxWithdraw,
                startBalance,
                _creditLimit,
                _commission);

            _accounts.Add(account);
            person.AddNewAccount(account);

            return account;
        }

        public void SetMaxTransfer(double amount)
        {
            var observers = new List<IAccount>();
            foreach (IAccount account in _accounts.Where(account => account.MaxTransfer != 0))
            {
                observers.Add(account);
                account.SetMaxTransfer(amount);
            }

            SendNotify(observers, amount, new TransferLimitMessage());
        }

        public void SetMaxWithdraw(double amount)
        {
            var observers = new List<IAccount>();
            foreach (IAccount account in _accounts.Where(account => account.MaxWithdraw != 0))
            {
                observers.Add(account);
                account.SetMaxWithdraw(amount);
            }

            SendNotify(observers, amount, new WithdrawLimitMessage());
        }

        public void SetCreditLimit(double amount)
        {
            var observers = new List<IAccount>();
            foreach (IAccount account in _accounts.Where(account => account.CreditLimit != 0))
            {
                observers.Add(account);
                account.SetCreditLimit(amount);
            }

            SendNotify(observers, amount, new CreditLimitMessage());
        }

        public void SetPercent(double amount)
        {
            var observers = new List<IAccount>();
            foreach (IAccount account in _accounts.Where(account => account.Percent != 0))
            {
                observers.Add(account);
                account.SetPercent(amount);
            }

            SendNotify(observers, amount, new PercentMessage());
        }

        public void Replenishment(IAccount account, double amount)
        {
            var trans = new ReplenishmentTransaction(account, amount, account.TransactionId);
            account.AddTransaction(trans);
        }

        public void Withdraw(IAccount account, double amount)
        {
            if (account.AccountPeriod != DateTime.MinValue && account.AccountPeriod < DateTime.Today)
            {
                throw new NotEndedDepositAccountException();
            }

            var trans = new WithdrawTransaction(account, amount, account.TransactionId);
            account.AddTransaction(trans);
        }

        public void Transfer(IAccount sender, IAccount recipient, double amount)
        {
            if (sender.AccountPeriod != DateTime.MinValue && sender.AccountPeriod < DateTime.Today)
            {
                throw new NotEndedDepositAccountException();
            }

            var trans = new TransferTransaction(sender, recipient, amount, sender.TransactionId);
            sender.AddTransaction(trans);
            recipient.AddTransaction(trans);
        }

        public void Cancellation(IAccount account, Transaction transaction)
        {
            var trans = new CancelTransaction(transaction);
            account.AddTransaction(trans);
        }

        public void UpdateBalance(DateTime dateTime)
        {
            foreach (IAccount account in _accounts.Where(account => account.AccountPeriod != DateTime.MinValue))
            {
                account.BalanceUpdate(dateTime);
            }
        }

        public ReadOnlyCollection<IAccount> GetAccounts()
        {
            return _accounts.AsReadOnly();
        }

        private double ChooseDepositPercent(double balance)
        {
            return _percentsBorders.FirstOrDefault(pair => balance < pair.Value).Key;
        }
    }
}