import { Permission } from './permissions';

export interface CapabilitiesContract {
  userId: string;
  roles: string[];
  permissions: Permission[];
  scopes: {
    spreadsheetIds: string[];
    chatSpaceIds: string[];
    zohoOrgIds: string[];
    paymentAccountIds: string[];
  };
  stepUpFresh: boolean;
}
