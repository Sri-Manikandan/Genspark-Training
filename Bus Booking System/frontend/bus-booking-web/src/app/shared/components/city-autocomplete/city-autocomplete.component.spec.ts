import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { environment } from '../../../../environments/environment';
import { CityAutocompleteComponent } from './city-autocomplete.component';

describe('CityAutocompleteComponent', () => {
  let fixture: ComponentFixture<CityAutocompleteComponent>;
  let component: CityAutocompleteComponent;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CityAutocompleteComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideNoopAnimations()]
    }).compileComponents();

    fixture = TestBed.createComponent(CityAutocompleteComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('label', 'From');
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('does not query when input is shorter than 2 chars', fakeAsync(() => {
    component.control.setValue('b');
    tick(250);
    http.expectNone(() => true);
    expect(component.options()).toEqual([]);
  }));

  it('queries the API after the debounce window', fakeAsync(() => {
    component.control.setValue('ban');
    tick(250);
    const req = http.expectOne(r =>
      r.url === `${environment.apiBaseUrl}/cities` && r.params.get('q') === 'ban');
    req.flush([{ id: 'c1', name: 'Bangalore', state: 'Karnataka', isActive: true }]);
    expect(component.options()).toEqual([
      { id: 'c1', name: 'Bangalore', state: 'Karnataka', isActive: true }
    ]);
  }));

  it('emits the selected city through `citySelected`', fakeAsync(() => {
    let emitted: unknown = null;
    component.citySelected.subscribe(c => (emitted = c));
    component.control.setValue('che');
    tick(250);
    http.expectOne(() => true).flush([
      { id: 'c2', name: 'Chennai', state: 'Tamil Nadu', isActive: true }
    ]);
    component.onSelect({ id: 'c2', name: 'Chennai', state: 'Tamil Nadu', isActive: true });
    expect(emitted).toEqual({ id: 'c2', name: 'Chennai', state: 'Tamil Nadu', isActive: true });
  }));
});
