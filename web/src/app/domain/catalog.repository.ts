import { CatalogBook } from './catalog';
import { Page } from './paging';

/** Port for discovering books in the external catalog (proxied through our API). */
export abstract class CatalogRepository {
  abstract search(term: string, page: number, pageSize: number): Promise<Page<CatalogBook>>;
}
